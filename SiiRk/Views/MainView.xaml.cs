using SiiRk.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace SiiRk.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            Title = "Рубежный контроль";

            DataContext = new ApplicationViewModel();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
