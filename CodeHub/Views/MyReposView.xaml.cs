using GalaSoft.MvvmLight.Messaging;
using CodeHub.Helpers;
using CodeHub.ViewModels;
using Octokit;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.System.Profile;
using Windows.Devices.Input;
using Windows.UI.Xaml.Controls;

namespace CodeHub.Views
{

    public sealed partial class MyReposView : Windows.UI.Xaml.Controls.Page
    {
        public MyReposViewmodel ViewModel { get; set; }
        private ScrollViewer RepoScrollViewer;
        private ScrollViewer StarredRepoScrollViewer;

        public MyReposView()
        {
            this.InitializeComponent();
            ViewModel = new MyReposViewmodel();
            this.DataContext = ViewModel;
           
            Loading += MyReposView_Loading;

            NavigationCacheMode = NavigationCacheMode.Required;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            RepoListView.SelectedIndex = StarredRepoListView.SelectedIndex = -1;

            ViewModel.User = (User)e.Parameter;

            MouseCapabilities mouseCapabilities = new MouseCapabilities();
            bool hasMouse = mouseCapabilities.MousePresent != 0;

            RepoListView.IsPullToRefreshWithMouseEnabled = StarredRepoListView.IsPullToRefreshWithMouseEnabled = hasMouse;
        }
        private async void MyReposView_Loading(FrameworkElement sender, object args)
        {
            Messenger.Default.Register<User>(this, ViewModel.RecieveSignInMessage); //Listening for Sign In message
            Messenger.Default.Register<GlobalHelper.SignOutMessageType>(this, ViewModel.RecieveSignOutMessage); //listen for sign out message
            await ViewModel.Load();
        }
        private void AllRepos_PullProgressChanged(object sender, Microsoft.Toolkit.Uwp.UI.Controls.RefreshProgressEventArgs e)
        {
            refreshindicator.Opacity = e.PullProgress;
            refreshindicator.Background = e.PullProgress < 1.0 ? GlobalHelper.GetSolidColorBrush("4078C0FF") : GlobalHelper.GetSolidColorBrush("47C951FF");
        }
        private void StarredRepos_PullProgressChanged(object sender, Microsoft.Toolkit.Uwp.UI.Controls.RefreshProgressEventArgs e)
        {
            refreshindicator2.Opacity = e.PullProgress;
            refreshindicator2.Background = e.PullProgress < 1.0 ? GlobalHelper.GetSolidColorBrush("4078C0FF") : GlobalHelper.GetSolidColorBrush("47C951FF");
        }

        private void RepoListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (RepoScrollViewer != null)
                RepoScrollViewer.ViewChanged -= OnRepoScrollViewerViewChanged;

            RepoScrollViewer = RepoListView.FindChild<ScrollViewer>();
            RepoScrollViewer.ViewChanged += OnRepoScrollViewerViewChanged;
        }

        private void StarredRepoListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (StarredRepoScrollViewer != null)
                StarredRepoScrollViewer.ViewChanged -= OnStarredRepoScrollViewerViewChanged;

            StarredRepoScrollViewer = StarredRepoListView.FindChild<ScrollViewer>();
            StarredRepoScrollViewer.ViewChanged += OnStarredRepoScrollViewerViewChanged;
        }

        private async void OnRepoScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (ViewModel.UserReposIndex != -1)
            {
                ScrollViewer sv = (ScrollViewer)sender;

                var verticalOffset = sv.VerticalOffset;
                var maxVerticalOffset = sv.ScrollableHeight; //sv.ExtentHeight - sv.ViewportHeight;

                if ((maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset) && verticalOffset > ViewModel.UserReposMaxScrollViewerOffset)
                {
                    ViewModel.UserReposMaxScrollViewerOffset = maxVerticalOffset;

                    // Scrolled to bottom
                    if (GlobalHelper.IsInternet())
                    {
                        ViewModel.isLoading = true;
                        await ViewModel.LoadRepos();
                    }
                    ViewModel.isLoading = false;
                }
            }
        }

        private async void OnStarredRepoScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (ViewModel.StarredUserReposIndex != -1)
            {
                ScrollViewer sv = (ScrollViewer)sender;

                var verticalOffset = sv.VerticalOffset;
                var maxVerticalOffset = sv.ScrollableHeight; //sv.ExtentHeight - sv.ViewportHeight;

                if ((maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset) && verticalOffset > ViewModel.StarredUserReposMaxScrollViewerOffset)
                {
                    ViewModel.StarredUserReposMaxScrollViewerOffset = maxVerticalOffset;

                    // Scrolled to bottom
                    if (GlobalHelper.IsInternet())
                    {
                        ViewModel.IsStarredLoading = true;
                        await ViewModel.LoadStarRepos();
                    }
                    ViewModel.IsStarredLoading = false;  
                }
            }
        }
    }
}
