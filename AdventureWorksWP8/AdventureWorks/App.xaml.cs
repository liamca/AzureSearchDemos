using System.Collections.Generic;
using BuildSessions.Common;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using BuildSessions.Views;
using BuildSessions.ViewModels;

// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace BuildSessions
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;

        public static Frame RootFrame { get; private set; }

        private static MainViewModel _mainViewModel;

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel MainViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (_mainViewModel == null)
                    _mainViewModel = new MainViewModel();

                return _mainViewModel;
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            InitializeApp();

            var storageFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///VoiceCommands.xml"));
            await Windows.Media.SpeechRecognition.VoiceCommandManager.InstallCommandSetsFromStorageFileAsync(storageFile);
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            InitializeApp();

            // Was the app activated by a voice command?
            if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.VoiceCommand)
            {
                var commandArgs = args as Windows.ApplicationModel.Activation.VoiceCommandActivatedEventArgs;
                Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = commandArgs.Result;

                // If so, get the name of the voice command and the values for the semantic properties from the grammar file
                string voiceCommandName = speechRecognitionResult.RulePath[0];
                var interpretation = speechRecognitionResult.SemanticInterpretation.Properties;
                IReadOnlyList<string> dictatedSearchTerms;
                interpretation.TryGetValue("dictatedSearchTerms", out dictatedSearchTerms);

                switch (voiceCommandName)
                {
                    case "CustomerSearch":
                        MainViewModel.SearchTerm = dictatedSearchTerms[0];
                        MainViewModel.SearchSessionsCommand.Execute(null);
                        break;

                    default:
                        // There is no match for the voice command name.
                        break;
                }
            }
        }

        private void InitializeApp()
        {
            RootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active.
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page.
                RootFrame = new Frame();

                // Associate the frame with a SuspensionManager key.
                SuspensionManager.RegisterFrame(RootFrame, "AppFrame");

                // Place the frame in the current Window.
                Window.Current.Content = RootFrame;
            }

            if (RootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (RootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in RootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                RootFrame.ContentTransitions = null;
                RootFrame.Navigated += this.RootFrame_FirstNavigated;

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter.
                if (!RootFrame.Navigate(typeof(SearchPage)))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active.
            Window.Current.Activate();
        }


        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
