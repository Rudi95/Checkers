using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Win32;

namespace Damajatek
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string startPath = Directory.GetCurrentDirectory(); //System.Windows.Forms.Application.StartupPath;
        public MainWindow()
        {
            InitializeComponent();
            ImageBrush myBrush = new ImageBrush(new BitmapImage(new Uri(startPath + "\\Images\\main_Background.png", UriKind.Relative)));
            myBrush.Opacity = 0.4;
            Grid.Background = myBrush;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings();
            settings.ShowDialog();
        }

        private void Rank_Click(object sender, RoutedEventArgs e)
        {
            Statistics stat = new Statistics();
            stat.Show();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = startPath + "\\Saves";
            ofd.Filter = "Szövegfájlok(*.sav)|*.sav";
            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName;
                GameScreen gameScreen = new GameScreen(filePath);
                gameScreen.ShowDialog();
            }
        }

        private void New_Game_Click(object sender, RoutedEventArgs e)
        {
            GameScreen gameScreen = new GameScreen();
            gameScreen.ShowDialog();
        }
    }
}
