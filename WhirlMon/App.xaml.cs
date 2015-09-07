using System;
using WhirlMonData;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;


namespace WhirlMonApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            try
            {
                WhirlMonData.WhirlPoolAPIClient.LoadConfig();
                WhirlMonWatchedTask.BackgroundTasks.Register();
            }
            catch (Exception x)
            {
                WhirlPoolAPIClient.ShowErrorToast("App", x);
            }
        }

        private void InitRoot(LaunchActivatedEventArgs e, string Arguments)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e != null && e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            InitRoot(e, e.Arguments);
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            WhirlPoolAPIClient.ShowToast("App.OnNavigationFailed: Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            try
            {
                WhirlMonData.WhirlPoolAPIClient.SaveConfig();
            }
            catch (Exception x)
            {
                WhirlPoolAPIClient.ShowErrorToast("App.OnSuspending", x);
            }

            deferral.Complete();
        }


        protected async override void OnActivated(IActivatedEventArgs args)
        {
            try
            {
                InitRoot(null, "");

                //Initialize your app if not initialized;
                //Find out if this is activated from a toast;
                if (args.Kind == ActivationKind.ToastNotification)
                {
                    var toastArgs = args as ToastNotificationActivatedEventArgs;
                    var argument = toastArgs.Argument;

                    string[] arguments = argument.Split(',');
                    string sid = arguments.Length > 0 ? arguments[0] : "";
                    string lastpage = arguments.Length > 1 ? arguments[1] : "";
                    string lastread = arguments.Length > 2 ? arguments[2] : "";
                    int id = 0;
                    int.TryParse(sid, out id);

                    if (lastpage != "")
                    {
                        String url = string.Format(@"http://forums.whirlpool.net.au/forum-replies.cfm?t={0}&p={1}&#r{2}", sid, lastpage, lastread);
                        var uri = new Uri(url);
                        var success = await Windows.System.Launcher.LaunchUriAsync(uri);
                        if (success)
                            await WhirlMonData.WhirlPoolAPIClient.MarkThreadReadAsync(id, false);
                    }
                    else
                    {
                        await WhirlMonData.WhirlPoolAPIClient.MarkThreadReadAsync(id, false);
                        await WhirlPoolAPIClient.GetWatchedAsync();
                    }
                }
            }
            catch (Exception x)
            {
                WhirlPoolAPIClient.ShowErrorToast("App.OnActivated", x);
            }
        }
    }
}
