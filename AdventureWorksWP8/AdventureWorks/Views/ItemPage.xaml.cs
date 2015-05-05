using BuildSessions.Common;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Maps;

namespace BuildSessions.Views
{
    public sealed partial class ItemPage : BasePage
    {
        public ItemPage()
        {
            InitializeComponent();
            NavigationHelper.LoadState += NavigationHelper_LoadState;
        }

        void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var index = e.NavigationParameter as int?;

            if (index != null)
            {
                var item = App.MainViewModel.Items[index.Value];
                DataContext = item;
            }
        }
    }
}