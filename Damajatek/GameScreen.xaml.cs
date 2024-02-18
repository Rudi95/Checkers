using Damajatek.Renderer;
using Damajatek.Controller;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;

namespace Damajatek
{
    /// <summary>
    /// Interaction logic for GameScreen.xaml
    /// </summary>
    public partial class GameScreen : Window
    {
        GameController gameControl;

        public GameScreen() // New Game
        {
            InitializeComponent();
            GameLogic logic = new GameLogic("new");
            display.Init(logic);
            gameControl = new GameController(logic);
        }

        public GameScreen(string path) //Load Game
        {
            InitializeComponent();
            string content = File.ReadAllText(path);
            GameLogic logic = JsonConvert.DeserializeObject<GameLogic>(content);
            display.Init(logic);
            gameControl = new GameController(logic);
            display.TurnedUpsideDown = logic.Turned;
            if (logic.Turned)
            {
                display.Angle = 180;
            }
        }

        private void Display_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double px = e.GetPosition(this.display).X;
            double py = e.GetPosition(this.display).Y;

            int i, j;

            if (display.TurnedUpsideDown)
            {
                double centerX = grid.ActualWidth / 2;
                double centerY = grid.ActualHeight / 2;
                double newx = centerX + (centerX - px);
                double newy = centerY + (centerY - py);

                j = (int)((newx - display.TileWidth) / display.TileWidth);
                i = (int)((newy - display.TileHeight) / display.TileHeight);
            }
            else
            {
                j = (int)((px - display.TileWidth) / display.TileWidth);
                i = (int)((py - display.TileHeight) / display.TileHeight);
            }

            gameControl.SelectFigure(i, j);

        }
        private void Display_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            double px = e.GetPosition(this.display).X;
            double py = e.GetPosition(this.display).Y;

            int i, j;

            if (display.TurnedUpsideDown)
            {
                double centerX = grid.ActualWidth / 2;
                double centerY = grid.ActualHeight / 2;

                double newx = centerX + (centerX - px);
                double newy = centerY + (centerY - py);

                j = (int)((newx - display.TileWidth) / display.TileWidth);
                i = (int)((newy - display.TileHeight) / display.TileHeight);
            }
            else
            {
                j = (int)((px - display.TileWidth) / display.TileWidth);
                i = (int)((py - display.TileHeight) / display.TileHeight);
            }

            gameControl.Move(i, j);

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            display.Resize(new Size(grid.ActualWidth, grid.ActualHeight));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            display.Resize(new Size(grid.ActualWidth, grid.ActualHeight));
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {            
            double px = e.GetPosition(this.display).X;
            double py = e.GetPosition(this.display).Y;

            double centerX = grid.ActualWidth / 2;
            double centerY = grid.ActualHeight / 2;

            if (display.TurnedUpsideDown)
            {
                double newx = centerX + (centerX - px);
                double newy = centerY + (centerY - py);

                display.MouseMov(newx, newy);
            }
            else
                display.MouseMov(px, py);
        }

        private void New_Game_Click(object sender, RoutedEventArgs e)
        {
            GameLogic logic = new GameLogic("new");
            display.Init(logic);
            gameControl = new GameController(logic);
        }

        private void Save_Game_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory() + "\\Saves",
                Filter = "Szövegfájlok(*.sav)|*.sav"
            };
            saveFileDialog.ShowDialog();
            gameControl.SaveGame(saveFileDialog.FileName, display.TurnedUpsideDown);
        }

        private void Load_Game_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Directory.GetCurrentDirectory() + "\\Saves";
            ofd.Filter = "Szövegfájlok(*.sav)|*.sav";
            if (ofd.ShowDialog() == true)
            {
                string filePath = ofd.FileName; 
                string content = File.ReadAllText(filePath);
                GameLogic logic = JsonConvert.DeserializeObject<GameLogic>(content);
                display.Init(logic);
                gameControl = new GameController(logic);
                display.TurnedUpsideDown = logic.Turned;
                if (logic.Turned)
                {
                    display.Angle = 180;
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Changing the setting results in starting a new game!", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (messageBoxResult == MessageBoxResult.OK)
            {
                Settings settings = new Settings();
                if (settings.ShowDialog() == true)
                {
                    GameLogic logic = new GameLogic("new");
                    display.Init(logic);
                    gameControl = new GameController(logic);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {            
            this.Close();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string text = "You can select your figure with LMB and make a move with LMB.";
            MessageBox.Show(text);
        }

        private void Statictics_Click(object sender, RoutedEventArgs e)
        {
            Statistics stat = new Statistics();
            stat.Show();
        }        
    }
}
