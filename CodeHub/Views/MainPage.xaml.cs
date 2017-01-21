﻿using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using CodeHub.Helpers;
using CodeHub.Models;
using CodeHub.Services;
using CodeHub.ViewModels;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using static CodeHub.Helpers.GlobalHelper;
using Octokit;
using CodeHub.Controls;

namespace CodeHub.Views
{
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        public MainViewmodel ViewModel { get; set; }
        public CustomFrame AppFrame { get { return this.mainFrame; } }
        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = new MainViewmodel();
            this.DataContext = ViewModel;

            SizeChanged += MainPage_SizeChanged;

            Messenger.Default.Register<NoInternetMessageType>(this, ViewModel.RecieveNoInternetMessage); //Listening for No Internet message
            Messenger.Default.Register<HasInternetMessageType>(this, ViewModel.RecieveInternetMessage); //Listening Internet available message
            Messenger.Default.Register(this, delegate(SetHeaderTextMessageType m)
            {
                ViewModel.setHeadertext(m.PageName);
            });  //Setting Header Text to the current page name

            SimpleIoc.Default.Register<INavigationService>(() =>
            { return new NavigationService(mainFrame); });
            SimpleIoc.Default.GetInstance<INavigationService>().Navigate(typeof(HomeView));
            NavigationCacheMode = NavigationCacheMode.Enabled;

            SystemNavigationManager.GetForCurrentView().BackRequested += SystemNavigationManager_BackRequested;
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(e.NewSize.Width < 720)
            {
                if (ViewModel.isLoggedin)
                {
                    BottomAppBar.Visibility = Visibility.Visible;
                }
                else
                {
                    BottomAppBar.Visibility = Visibility.Collapsed;
                }
            }
        }
        private async void SystemNavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (this.AppFrame == null)
                return;

            if (this.AppFrame.CanGoBack && !e.Handled)
            {
                e.Handled = true;
                await this.AppFrame.GoBack();
            }
        }
        private void HamButton_Click(object sender, RoutedEventArgs e)
        {
            HamSplitView.IsPaneOpen = !HamSplitView.IsPaneOpen;
        }
        private void HamListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.HamItemClicked(e.ClickedItem as HamItem);
            HamSplitView.IsPaneOpen = false;
        }
        private void SettingsItem_ItemClick(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.NavigateToSettings();
            HamSplitView.IsPaneOpen = false;
        }
        private void AppBarTrending_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.HamItemClicked(ViewModel.HamItems[0]);
        }
        private void AppBarProfile_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.HamItemClicked(ViewModel.HamItems[1]);
        }
        private void AppBarMyRepos_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.HamItemClicked(ViewModel.HamItems[2]);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.isLoggedin = (bool)e.Parameter;

            //Listening for Sign In message
            Messenger.Default.Register<User>(this, RecieveSignInMessage);

            //listen for sign out message
            Messenger.Default.Register<SignOutMessageType>(this, RecieveSignOutMessage);

            /* This has to be done because the visibilty of BottomAppBar
             * is dependent on screen size as well as isLoggedin property
             * If visibility is bound with isLoggedin, it will disregard 
             * VisualStateManager at first loading of Page.
             * */
            if (Window.Current.Bounds.Width < 720)
            {
                if (ViewModel.isLoggedin)
                {
                    BottomAppBar.Visibility = Visibility.Visible;
                }
                else
                {
                    BottomAppBar.Visibility = Visibility.Collapsed;
                }
            }
        }
        public void RecieveSignOutMessage(SignOutMessageType empty)
        {
            if (Window.Current.Bounds.Width < 720)
            {
                BottomAppBar.Visibility = Visibility.Collapsed;
            }
        }
        public void RecieveSignInMessage(User user)
        {
            if (Window.Current.Bounds.Width < 720)
            {
                BottomAppBar.Visibility = Visibility.Visible;
            }
        }
    }
}
