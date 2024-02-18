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
using System.IO;
using System.Globalization;
using Damajatek.Logic;

namespace Damajatek
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private Player P1;
        private Player P2;
        public Settings()
        {
            InitializeComponent();
             P1 = new Player();
             P2 = new Player();
            SP_Player_1.DataContext = P1;
            SP_Player_2.DataContext = P2;
            RB_1.DataContext = P1;
            RB_2.DataContext = P2;

            string[] s = File.ReadAllLines("Settings.dat");
            string[,] st = new string[s.Length, 2];
            for (int i = 0; i < s.Length; i++)
            {
                string[] str = s[i].Split('=');
                st[i, 0] = str[0];
                st[i, 1] = str[1];
            }
            if (st[0, 0] == "aigame" && st[0, 1] == "True")
            {
                P1.AI = true;
                P2.AI = false;
            }
            else if (st[0, 1] == "False")
            {
                P1.AI = false;
                P2.AI = true;
            }

            P1.Name = st[1, 1];
            P2.Name = st[2, 1];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(P1.Name.Length < 1)
            {
                tb_nev_1.Background = Brushes.Red;
            }            
            if(P2.Name.Length < 1 && !P1.AI)
            {
                tb_nev_2.Background = Brushes.Red;
            }            
            if (P1.AI)
            {
                P2.Name = string.Empty;                
            }
            if ((P1.Name.Length > 0 && P2.Name.Length > 0) || (P1.AI && P2.Name.Length == 0))
            {
                string s = "aigame=" + P1.AI + "\nplayer1=" + P1.Name + "\nplayer2=" + P2.Name;
                File.WriteAllText("Settings.dat", s);
                DialogResult = true;
                this.Close();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Tb_nev_1_KeyDown(object sender, KeyEventArgs e)
        {
            if ( tb_nev_1.Text.Length>0 && tb_nev_1.Background == Brushes.Red )
            {
                tb_nev_1.Background = Brushes.Beige;
            }
        }

        private void Tb_nev_2_KeyDown(object sender, KeyEventArgs e)
        {
            if (tb_nev_2.Text.Length > 0 && tb_nev_2.Background == Brushes.Red)
            {
                tb_nev_2.Background = Brushes.Beige;
            }
        }
    }
}
