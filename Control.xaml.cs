using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Torch.Views;

namespace RandomWeatherPlugin
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Control : UserControl
    {
        
        private RandomWeatherPluginCore Plugin { get; }

        public Control()
        {
            InitializeComponent();
        }

        public Control(RandomWeatherPluginCore plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }

        private void EditExceptedWeathers_OnClick(object sender, RoutedEventArgs e)
        {
            var editor = new CollectionEditor() {Owner = Window.GetWindow(this)};
            editor.Edit<string>(Plugin.Config.ExceptedWeathers, "Weather SubTypeNames");
        }

        private void EditExceptedPlanets_OnClick(object sender, RoutedEventArgs e)
        {
            var editor = new CollectionEditor() {Owner = Window.GetWindow(this)};
            editor.Edit<string>(Plugin.Config.ExceptedPlanets, "Planet Names");
        }



    }
}
