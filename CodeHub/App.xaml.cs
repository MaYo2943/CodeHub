﻿using CodeHub.Helpers;
using CodeHub.Services;
using CodeHub.Services.Hilite_me;
using CodeHub.ViewModels;
using CodeHub.Views;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.QueryStringDotNET;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using XamlBrewer.Uwp.Controls;
using static CodeHub.Helpers.GlobalHelper;
using static CodeHub.Helpers.StringHelper;
using Issue = Octokit.Issue;
using PullRequest = Octokit.PullRequest;
using Repository = Octokit.Repository;


namespace CodeHub
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private BackgroundTaskDeferral _SyncDeferral;
        private BackgroundTaskDeferral _ToastActionDeferral;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            Suspending += OnSuspending;
            UnhandledException += Application_UnhandledException;

            // Theme setup
            RequestedTheme = SettingsService.Get<bool>(SettingsKeys.AppLightThemeEnabled) ? ApplicationTheme.Light : ApplicationTheme.Dark;
            SettingsService.Save(SettingsKeys.HighlightStyleIndex, (int)SyntaxHighlightStyleEnum.Monokai, false);
            SettingsService.Save(SettingsKeys.ShowLineNumbers, true, false);
            SettingsService.Save(SettingsKeys.LoadCommitsInfo, false, false);
            SettingsService.Save(SettingsKeys.IsAdsEnabled, false, false);
            SettingsService.Save(SettingsKeys.IsNotificationCheckEnabled, true, false);
            SettingsService.Save(SettingsKeys.HasUserDonated, false, false);


            AppCenter.Start("ecd96e4c-b301-48f3-b640-166a040f1d86", typeof(Analytics), typeof(Crashes));
        }

        private async void Application_UnhandledException(
            object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            await CoreApplication.MainView?.CoreWindow?.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await new MessageDialog(e.ToString(), e.Message).ShowAsync();
            });
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            OnLaunchedOrActivated(args);
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var taskInstance = args.TaskInstance;
            taskInstance.Canceled += TaskInstance_Canceled;
            taskInstance.Task.Completed += BackgroundTask_Completed;
            var taskName = args.TaskInstance.Task.Name;
            switch (taskName)
            {
                case "SyncNotifications":
                case "SyncNotificationsApp":
                    _SyncDeferral = taskInstance.GetDeferral();
                    //await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    //{
                    //await new MessageDialog("sync activated").ShowAsync();

                    AppViewmodel.UnreadNotifications = await NotificationsService.GetAllNotificationsForCurrentUser(false, false);
                    await SendMessage(new UpdateUnreadNotificationsCountMessageType { Count = AppViewmodel.UnreadNotifications?.Count ?? 0 });
                    //});
                    break;
                case "ToastNotificationBackgroundTask":
                    _ToastActionDeferral = taskInstance.GetDeferral();
                    var toastTriggerDetails = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
                    var toastArgs = QueryString.Parse(toastTriggerDetails.Argument);
                    var notificationId = toastArgs["notificationId"];
                    if (!string.IsNullOrEmpty(notificationId) && !string.IsNullOrWhiteSpace(notificationId))
                    {
                        await NotificationsService.MarkNotificationAsRead(notificationId);
                        var toast = await (await NotificationsService.GetNotificationById(notificationId)).BuildToast(ToastNotificationScenario.Reminder);
                        ToastNotificationManager.History.Remove(toast.Tag, toast.Group);
                    }

                    break;
            }
            //deferral.Complete();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            //    CoreDispatcherPriority.High,
            //    async () =>
            //    {
            //        await new MessageDialog(reason.ToString()).ShowAsync();
            //    });
            BackgroundTaskStorage.PutError(reason.ToString());
            switch (sender.Task.Name)
            {
                case "SyncNotifications":
                case "SyncNotificationsApp":
                    _SyncDeferral?.Complete();
                    break;
                case "ToastNotificationBackgroundTask":
                    _ToastActionDeferral?.Complete();
                    break;
            }
        }

        private async Task SendMessage<T>(T messageType)
            where T : MessageTypeBase, new()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Messenger.Default?.Send(messageType);
            });
        }

        private async void BackgroundTask_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            //args.CheckResult(); 
            switch (sender.Name)
            {
                case "SyncNotifications":
                case "SyncNotificationsApp":
                    _SyncDeferral?.Complete();
                    break;

                case "ToastNotificationBackgroundTask":
                    AppViewmodel.UnreadNotifications = await NotificationsService.GetAllNotificationsForCurrentUser(false, false);
                    await SendMessage(new UpdateUnreadNotificationsCountMessageType { Count = AppViewmodel.UnreadNotifications?.Count ?? 0 });
                    _ToastActionDeferral?.Complete();
                    break;
            }
        }

        /// <summary>
        /// Invoked when the application is  launched normally by the end user. Other entry points will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the         launch request and process.
        /// </param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            OnLaunchedOrActivated(e);
        }

        private void Activate()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
            {
                var view = ApplicationView.GetForCurrentView();
                view.SetPreferredMinSize(new Size(width: 800, height: 600));

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                if (titleBar != null)
                {
                    titleBar.ButtonBackgroundColor = titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                    titleBar.ForegroundColor =
                    titleBar.ButtonInactiveForegroundColor =
                    titleBar.ButtonForegroundColor =
                    Colors.White;
                }
            }
        }

        private async void OnLaunchedOrActivated(IActivatedEventArgs args)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = true;
            }

#endif
            if (args is LaunchActivatedEventArgs launchArgs)
            {
                // Set the right theme-depending color for the alternating rows
                if (SettingsService.Get<bool>(SettingsKeys.AppLightThemeEnabled))
                {
                    XAMLHelper.AssignValueToXAMLResource("OddAlternatingRowsBrush", new SolidColorBrush { Color = Color.FromArgb(0x08, 0, 0, 0) });
                }
                if (!launchArgs.PrelaunchActivated)
                {
                    if (Window.Current.Content == null)
                    {
                        Window.Current.Content = new MainPage(null);
                        (Window.Current.Content as Page).OpenFromSplashScreen(launchArgs.SplashScreen.ImageLocation);
                    }
                }
                Activate();
            }
            else if (args is ToastNotificationActivatedEventArgs toastActivatedEventArgs)
            {
                if (args.Kind == ActivationKind.Protocol)
                {
                    if (args.PreviousExecutionState == ApplicationExecutionState.Running)
                    {
                        await HandleProtocolActivationArguments(args);
                    }
                    else
                    {
                        if (SettingsService.Get<bool>(SettingsKeys.AppLightThemeEnabled))
                        {
                            XAMLHelper.AssignValueToXAMLResource("OddAlternatingRowsBrush", new SolidColorBrush { Color = Color.FromArgb(0x08, 0, 0, 0) });
                        }

                        if (Window.Current.Content == null)
                        {
                            Window.Current.Content = new MainPage(args);
                        }

                        Activate();
                    }
                }
                else if (args.Kind == ActivationKind.ToastNotification)
                {
                    var toastArgs = QueryString.Parse(toastActivatedEventArgs.Argument);
                    var repoIdFromArg = toastArgs["repoId"];
                    if (repoIdFromArg.IsNulloremptyOrNullorwhitespace())
                    {
                        throw new ArgumentNullException("repoId");
                    }

                    if (!long.TryParse(repoIdFromArg, out long repoId))
                    {
                        throw new ArgumentException($"Invalid value passed in argument 'repoId'");
                    }

                    var repo = await RepositoryUtility.GetRepository(repoId);

                    if (SettingsService.Get<bool>(SettingsKeys.AppLightThemeEnabled))
                    {
                        XAMLHelper.AssignValueToXAMLResource("OddAlternatingRowsBrush", new SolidColorBrush { Color = Color.FromArgb(0x08, 0, 0, 0) });
                    }

                    if (Window.Current.Content == null)
                    {
                        Window.Current.Content = new MainPage(args);
                    }
                    else
                    {
                        var notificationId = toastArgs["notificationId"];
                        if (notificationId.IsNulloremptyOrNullorwhitespace())
                        {
                            throw new ArgumentNullException(nameof(notificationId));
                        }

                        var tag = $"N{notificationId}";
                        switch (toastArgs["action"])
                        {
                            case "showIssue":
                                var issueIdFromArg = toastArgs["issueId"];
                                if (notificationId.IsNulloremptyOrNullorwhitespace())
                                {
                                    throw new ArgumentNullException("issueId");
                                }

                                if (!int.TryParse(issueIdFromArg, out int issueId))
                                {
                                    throw new ArgumentException($"Invalid value passed in argument {nameof(issueId)}");
                                }

                                var issue = await IssueUtility.GetIssue(repo.Id, issueId);
                                await SimpleIoc
                                    .Default
                                    .GetInstance<IAsyncNavigationService>()
                                    .NavigateAsync(typeof(IssueDetailView), new Tuple<Repository, Issue>(repo, issue));
                                tag += $"+I{issue.Number}";
                                break;

                            case "showPR":
                                var prIdFromArg = toastArgs["prId"];
                                if (notificationId.IsNulloremptyOrNullorwhitespace())
                                {
                                    throw new ArgumentNullException("prId");
                                }

                                if (!int.TryParse(prIdFromArg, out int prId))
                                {
                                    throw new ArgumentException($"Invalid value passed in argument {nameof(prId)}");
                                }

                                var pr = await PullRequestUtility.GetPullRequest(repo.Id, prId);
                                await SimpleIoc
                                        .Default
                                        .GetInstance<IAsyncNavigationService>()
                                        .NavigateAsync(typeof(PullRequestDetailView), new Tuple<Repository, PullRequest>(repo, pr));
                                tag += $"+P{pr.Number}";
                                break;

                        }
                        tag += $"+R{repo.Id}";
                        ToastNotificationManager.History.Remove(tag);
                        if (!notificationId.IsNulloremptyOrNullorwhitespace())
                        {
                            await NotificationsService.MarkNotificationAsRead(notificationId);
                        }
                    }

                    Activate();
                }
                else if (args.Kind == ActivationKind.StartupTask)
                {
                    var startupArgs = args as StartupTaskActivatedEventArgs;
                    var payload = ActivationKind.StartupTask.ToString();
                    if (Window.Current.Content == null)
                    {
                        Window.Current.Content = new MainPage(args);
                    }
                    Activate();
                    (Window.Current.Content as Frame).Navigate(typeof(MainPage), payload);
                    return;
                }
            }
            Window.Current.Activate();

            IBackgroundCondition internetAvailableCondition = new SystemCondition(SystemConditionType.InternetAvailable),
                                 userPresentCondition = new SystemCondition(SystemConditionType.UserPresent),
                                 sessionConnectedCondition = new SystemCondition(SystemConditionType.SessionConnected),
                                 backgroundCostNotHighCondition = new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh);

            var syncBuilder = BackgroundTaskHelper.BuildBackgroundTask(
                        "SyncNotificationsApp",
                        AppViewmodel.AppTrigger,
                        internetAvailableCondition,
                        userPresentCondition,
                        sessionConnectedCondition
                      );
            syncBuilder.IsNetworkRequested = true;
            syncBuilder.Register();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            //throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
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
            //TODO: Save application state and stop any background activity

            deferral.Complete();
        }

        private async Task HandleProtocolActivationArguments(IActivatedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(UserLogin))
            {
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;

                switch (eventArgs.Uri.Host.ToLower())
                {
                    case "repository":
                        await SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(RepoDetailView), eventArgs.Uri.Segments[1] + eventArgs.Uri.Segments[2]);
                        break;

                    case "user":
                        await SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(DeveloperProfileView), eventArgs.Uri.Segments[1]);
                        break;

                }
            }
        }
    }
}