using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damajatek.Controller
{
    public class GameController
    {
        IGameControl logic;
        public GameController(IGameControl control)
        {
            logic = control;
        }

        public void SelectFigure(int i, int j)
        {
            logic.SelectFigure(i, j);
        }

        public void Move(int i, int j)
        {
            logic.Move(i, j);
        }
        
        public void SaveGame(string fileName, bool turned)
        { 
            logic.SaveGame(fileName, turned);
        }
    }
}
