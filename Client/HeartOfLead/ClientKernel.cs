﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using CommandLine;
using Marvin.ClientFramework.Dialog;
using Marvin.ClientFramework.Kernel;
using Marvin.ClientFramework.UI;
using Marvin.Container;
using Marvin.Logging;
using Marvin.Threading;
using Marvin.Tools;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace Marvin.ClientFramework.HeartOfLead
{
    /// <summary>
    /// Main kernel class to load the overall client framework. 
    /// Will instanciate the container and will start the whole lifecycle.
    /// </summary>
    public class ClientKernel : ILoggingHost
    {
        #region Fields

        private GlobalContainer _container;
        private IKernelConfigManager _configManager;
        private IAppDataConfigManager _appDataConfigManager;
        private BaseApplication _application;
        private IRunModeController _runModeController;
        private AppConfig _appConfig;
        private ILoaderHandler _loaderHandler;
        private IModuleManager _moduleManager;

        private readonly string[] _args;
        private ArgumentOptions _commandLineOptions;

        #endregion

        #region Imports

        /// <summary>
        /// Dll import to attach this application to the console stream
        /// </summary>
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        #endregion

        #region Main and ctor

        /// <summary>
        /// Main start point of this application
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            new ClientKernel(args).Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientKernel"/> class.
        /// </summary>
        public ClientKernel(string[] args)
        {
            _args = args;

            // This is necessary to enable assembly lookup from AppDomain
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            LoadAssembliesFromFramework();
        }

        #endregion

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            // Attach this Application to the console.
            AttachConsole(-1);

            // Will parse the exe arguments
            ParseCommandLineArguments();

            // Create global container and configure config manager
            CreateContainer();

            // Activates the logging
            ActivateKernelLogging();

            //Configures the thread context
            ConfigureThreadContext();
            
            // Register app config
            LoadConfiguration();

            // Limit instances
            LimitInstances();

            // Manages the loader
            ConfigureLoaderHandler();

            // Start the base application here - thread will hold up to that the application will closed
            StartApplication();
        }

        /// <summary>
        /// Parses the arguments.
        /// </summary>
        private void ParseCommandLineArguments()
        {
            var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
            var result = parser.ParseArguments<ArgumentOptions>(_args);
            if (result.Tag == ParserResultType.Parsed)
                _commandLineOptions = ((Parsed<ArgumentOptions>) result).Value;
            else
                Environment.Exit(1);
        }

        /// <summary>
        /// Configures the thread context.
        /// </summary>
        private void ConfigureThreadContext()
        {
            var catcher = _container.Resolve<IErrorCatcher>();

            //set global thread context
            ThreadContext.Dispatcher = Dispatcher.CurrentDispatcher;
            ThreadContext.Dispatcher.UnhandledException += catcher.HandleDispatcherException;
        }
        
        /// <summary>
        /// configures the loader handler (loader view)
        /// </summary>
        private void ConfigureLoaderHandler()
        {
            _loaderHandler = _container.Resolve<ILoaderHandler>();
            _container.ComponentCreated += _loaderHandler.CheckForAdapter;
            _loaderHandler.Initialize();
        }
        
        /// <summary>
        /// Activates the logging.
        /// </summary>
        private void ActivateKernelLogging()
        {
            var management = _container.Resolve<ILoggerManagement>();
            management.ActivateLogging(this);

            _container.SetInstance(((ILoggingHost)this).Logger);
        }

        /// <summary>
        /// Starts the application.
        /// </summary>
        private void StartApplication()
        {
            _application = new BaseApplication(_container);
            _application.InitializeComponent();
            _application.Startup += OnApplicationStartUp;
            _application.Exit += OnApplicationExit;

            _application.Run();
        }

        /// <summary>
        /// Called when [application start up].
        /// </summary>
        private void OnApplicationStartUp(object sender, StartupEventArgs startupEventArgs)
        {
            var cmpInitializer = _container.Resolve<IComponentInitializer>();
            cmpInitializer.Completed += CmpInitializerCompleted;

            cmpInitializer.Initialize();
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void OnApplicationExit(object sender, ExitEventArgs exitEventArgs)
        {
            _application.DisposeShell();

            if (_moduleManager != null)
                _moduleManager.Dispose();

            if (_appDataConfigManager != null)
                _appDataConfigManager.SaveAll();
        }

        /// <summary>
        /// Called when all components are initialized
        /// </summary>
        private void CmpInitializerCompleted(object sender, EventArgs eventArgs)
        {
            _runModeController = _container.Resolve<IRunModeController>();

            //configure runmode and initialize
            _runModeController.RunModeReady += OnRunModeReady;
            _runModeController.Initialize();

            _runModeController.Start();
        }

        /// <summary>
        /// Called when [run mode ready].
        /// </summary>
        private void OnRunModeReady(object sender, ModulesConfiguration modulesConfiguration)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    _moduleManager = _container.Resolve<IModuleManager>();
                    _moduleManager.Initialize();

                    ThreadContext.BeginInvoke(() => _application.InitializeShell());
                }
                catch (Exception exception)
                {
                    CrashHandler.WriteErrorToFile(ExceptionPrinter.Print(exception));
                    throw;
                }
            });
        }

        /// <summary>
        /// Creates the container
        /// </summary>
        private void CreateContainer()
        {
            var container = new GlobalContainer();

            // Parallel operations
            container.Register<IParallelOperations, ParallelOperations>();

            // Register runtimes
            container.Register<IWindowManager, WindowManager>();

            // Load kernel and core modules
            container.ExecuteInstaller(new AutoInstaller(GetType().Assembly));
            container.LoadComponents<object>(type => type.GetCustomAttribute(typeof(KernelComponentAttribute)) != null);

            //TODO: find better way to register factory and global components
            container.Register<IMessageBox, MessageBoxViewModel>("MessageBoxViewModel", LifeCycle.Transient);
            container.Register<IMessageBoxFactory>();

            _container = container;
        }

        /// <summary>
        /// Loads all framework configurations configurations.
        /// </summary>
        private void LoadConfiguration()
        {
            if (!Directory.Exists(_commandLineOptions.ConfigFolder))
                Directory.CreateDirectory(_commandLineOptions.ConfigFolder);

            //configure config manager
            _configManager = new KernelConfigManager { ConfigDirectory = _commandLineOptions.ConfigFolder };
            _container.SetInstance(_configManager);

            //load global app config
            _appConfig = _configManager.GetConfiguration<AppConfig>();

            _appDataConfigManager = new AppDataConfigManager();
            _appDataConfigManager.Initialize(_appConfig.Application);
            _container.SetInstance(_appDataConfigManager);
                       
            //Start configurator, if wanted
            if (_commandLineOptions.StartConfigurator || _appConfig.OpenConfigWithControl && (Keyboard.Modifiers & ModifierKeys.Control) > 0)
                _appConfig.RunMode = KernelConstants.CONFIG_RUNMODE;
        }

        /// <summary>
        /// If enabled, instances will be limited
        /// </summary>
        private void LimitInstances()
        {
            if (!_appConfig.LimitInstances)
                return;

            var instances = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location));
            if (instances.Length < 1)
                return;

            MessageBox.Show("Application is already started. Please close all windows before starting a new instance",
                "Already started", MessageBoxButton.OK, MessageBoxImage.Error);

            Environment.Exit(1);
        }

        /// <summary>
        /// Find a matching assembly in the AppDomain
        /// </summary>
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(item => item.GetName().Name == args.Name.Split(',')[0]);
        }

        /// <summary>
        /// Loads the assemblies from framework folder.
        /// </summary>
        public static void LoadAssembliesFromFramework()
        {
            // Load all additional directories
            AppDomainBuilder.LoadAssemblies();

            // Set working directory to location of this exe
            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        string ILoggingHost.Name
        {
            get { return "ClientFramework"; }
        }

        IModuleLogger ILoggingHost.Logger { get; set; }
    }
}