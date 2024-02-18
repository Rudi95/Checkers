using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Damajatek.Logic
{
    public class Bindable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OPC([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
   
    public class Player : Bindable
    {
        string name;
        bool aI;
        bool isWinner;
        bool isLooser;
        GameItem figure;

        public string Name 
        { get { return name; } 
            set { name = value; OPC(); } }
        public bool AI { get { return aI; } set { aI = value; OPC(); } }
        public bool IsWinner { get => isWinner; set => isWinner = value; }
        public bool IsLooser { get => isLooser; set => isLooser = value; }
        public GameItem Figure { get => figure; set => figure = value; }

        public Player(bool aI, GameItem figure, string name)
        {
            this.Name = name;
            this.aI = aI;
            isWinner = false;
            isLooser = false;
            this.Figure = figure;
        }
        public Player()
        {

        }
        /// <summary>
        /// Returns if the GameItem is the same color as the Player figures color
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Equals(GameItem item)
        {
            if (item == figure)
            {
                return true;
            }
            else if (item.ToString().Contains(figure.ToString()))
            {
                return true;
            }
            return false;
        }
    }
}
