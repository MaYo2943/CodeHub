﻿using System;
using System.Threading;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using CodeHub.Helpers;
using CodeHub.Models;
using CodeHub.Services;
using CodeHub.ViewModels;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using static CodeHub.Helpers.GlobalHelper;
using Octokit;
using CodeHub.Controls;
using UICompositionAnimations;
using UICompositionAnimations.Enums;
using Windows.UI.ViewManagement;
using RavinduL.LocalNotifications;
using RavinduL.LocalNotifications.Presenters;
using Windows.UI.Popups;
using Windows.System.Profile;
using UICompositionAnimations.Behaviours.Effects.Base;
using UICompositionAnimations.Behaviours;
using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Threading.Tasks;

namespace CodeHub.Views
{
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        public MainViewmodel ViewModel { get; set; }
        public CustomFrame AppFrame { get { return this.mainFrame; } }
        private readonly SemaphoreSlim HeaderAnimationSemaphore = new SemaphoreSlim(1);
        private LocalNotificationManager notifManager;
        AttachedStaticCompositionEffect<Border> HostWindowBlurEffect;

        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = new MainViewmodel();
            this.DataContext = ViewModel;

            #region registering for messages
            Messenger.Default.Register<LocalNotificationMessageType>(this, RecieveLocalNotificationMessage);
            Messenger.Default.Register(this, delegate(SetHeaderTextMessageType m) {  SetHeadertext(m.PageName); });
            Messenger.Default.Register(this, delegate (AdsEnabledMessageType m) { ConfigureAdsVisibility(); });
            Messenger.Default.Register(this, delegate (HostWindowBlurMessageType m) { ConfigureWindowBlur(); });
            #endregion

            SimpleIoc.Default.Register<IAsyncNavigationService>(() =>
            { return new NavigationService(mainFrame); });
            
            NavigationCacheMode = NavigationCacheMode.Enabled;
            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;
            ConfigureAdsVisibility();
        }

        private async void OnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            await HeaderText.StartCompositionFadeSlideAnimationAsync(1, 0, TranslationAxis.X, 0, 24, 150, null, null, EasingFunctionNames.Linear);
            await HeaderText.StartCompositionFadeSlideAnimationAsync(0, 1, TranslationAxis.X, 24, 0, 150, null, null, EasingFunctionNames.Linear);
        }
        private void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (AppFrame == null) return;
            IAsyncNavigationService service = SimpleIoc.Default.GetInstance<IAsyncNavigationService>();
            if (service != null && AppFrame.CanGoBack && !e.Handled) // The base CanGoBack is synchronous and not reliable here
            {
                e.Handled = true;
                service.GoBackAsync(); // Use the navigation service to make sure the navigation is possible
            }
        }

        #region click events
        private void HamButton_Click(object sender, RoutedEventArgs e)
        {
            //Toggle Hamburger menu
            HamSplitView.IsPaneOpen = !HamSplitView.IsPaneOpen;
        }
        private void HamListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (SimpleIoc.Default.GetInstance<IAsyncNavigationService>().CurrentSourcePageType != (e.ClickedItem as HamItem).DestPage)
            {
                ViewModel.HamItemClicked(e.ClickedItem as HamItem);

                //Don't close the Hamburger menu if visual state is DesktopEx
                if (!(HamSplitView.DisplayMode == SplitViewDisplayMode.Inline))
                    HamSplitView.IsPaneOpen = false;
            }
        }
        private void SettingsItem_ItemClick(object sender, TappedRoutedEventArgs e)
        {
            if (SimpleIoc.Default.GetInstance<IAsyncNavigationService>().CurrentSourcePageType != typeof(SettingsView))
            {
                ViewModel.NavigateToSettings();

                //Don't close the Hamburger menu if visual state is DesktopEx
                if (!(HamSplitView.DisplayMode == SplitViewDisplayMode.Inline))
                    HamSplitView.IsPaneOpen = false;
            }
        }
        private void SignOutFlyout_Tapped(object sender, TappedRoutedEventArgs e)
        {
            moreButton.Flyout.Hide();
            ViewModel.SignOutCommand.Execute(null);
        }
        #endregion

        #region Messaging
        public void RecieveLocalNotificationMessage(LocalNotificationMessageType notif)
        {
            notifManager.Show(new SimpleNotificationPresenter
            (
                TimeSpan.FromSeconds(3),
                text: notif.Message,
                action: async () => await new MessageDialog(notif.Message).ShowAsync(),
                glyph: notif.Glyph
            )
            {
                Background = GetSolidColorBrush("60B53BFF"),
                Foreground = GetSolidColorBrush("FAFBFCFF"),
            },
            LocalNotificationCollisionBehaviour.Replace);
        }
        public void RecieveSignInMessage(User user)
        {
            if (SimpleIoc.Default.GetInstance<IAsyncNavigationService>().CurrentSourcePageType != typeof(HomeView))
            {
                SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(HomeView), "Trending");
            }
        }
        #endregion

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.isLoggedin = (bool)e.Parameter;

            if (ViewModel.isLoggedin)
            {
                SimpleIoc.Default.GetInstance<IAsyncNavigationService>().NavigateAsync(typeof(HomeView), "Trending");
            }

            //Listening for Sign In message
            Messenger.Default.Register<User>(this, RecieveSignInMessage);

            notifManager = new LocalNotificationManager(NotificationGrid);

            await ConfigureWindowBlur();

            await BlurBorderHamburger.GetAttachedInAppSemiAcrylicEffectAsync(BlurBorderHamburger,8,100, ((SolidColorBrush)App.Current.Resources["ApplicationPageBackgroundThemeBrush"]).Color, 0.6f,Win2DCanvas, new Uri("ms-appx:///Assets/Noise.png"));
        }

        #region other methods

        /// <summary>
        /// Sets the Header Text to pageName
        /// </summary>
        /// <param name="pageName"></param>
        public async void SetHeadertext(string pageName)
        {
            await HeaderAnimationSemaphore.WaitAsync();
            if (ViewModel.HeaderText?.Equals(pageName.ToUpper()) != true)
            {
                await HeaderText.StartCompositionFadeSlideAnimationAsync(1, 0, TranslationAxis.X, 0, 24, 150, null, null, EasingFunctionNames.Linear);
                ViewModel.HeaderText = pageName.ToUpper();
                await HeaderText.StartCompositionFadeSlideAnimationAsync(0, 1, TranslationAxis.X, 24, 0, 150, null, null, EasingFunctionNames.Linear);
            }
            HeaderAnimationSemaphore.Release();
        }

        /// <summary>
        /// Sets the visibility of Ad units according to the app settings
        /// </summary>
        public void ConfigureAdsVisibility()
        {
            if (SettingsService.Get<bool>(SettingsKeys.IsAdsEnabled))
            {
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
                {
                    adControlDesktop.Visibility = Visibility.Collapsed;
                    adControlMobile.Visibility = Visibility.Visible;
                }
                else
                {
                    adControlMobile.Visibility = Visibility.Collapsed;
                    adControlDesktop.Visibility = Visibility.Visible;
                }
            }
            else
            {
                adControlMobile.Visibility = adControlDesktop.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Sets Acrylic blur effect for host window and hamburger menu pane
        /// </summary>
        /// <returns></returns>
        public async Task ConfigureWindowBlur()
        {
            if (SettingsService.Get<bool>(SettingsKeys.IsAcrylicBlurEnabled))
            {
                if (GetOSBuild() >= 10563 || AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile")
                {
                   HostWindowBlurEffect = await BlurBorder.GetAttachedSemiAcrylicEffectAsync(
                                          ((SolidColorBrush)App.Current.Resources["ApplicationPageBackgroundThemeBrush"]).Color,
                                          0.8f,
                                          Win2DCanvas,
                                          new Uri("ms-appx:///Assets/transparent.png"));
                    BlurBorder.SizeChanged += (s, er) => HostWindowBlurEffect.AdjustSize();
                }
            }
            else
            {
                if (HostWindowBlurEffect != null)
                {
                    //Remove the Host Window blur effect
                    HostWindowBlurEffect.Dispose();
                }
            }
        }

        #endregion
    }
}
