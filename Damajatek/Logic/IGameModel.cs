using static Damajatek.GameLogic;
using System.Collections.Generic;
using Damajatek.Logic;
using System;

namespace Damajatek
{
    public interface IGameModel
    {
        Player Light { get; set; }
        Player Dark { get; set; }
        Queue<Player> NextPlayer { get; set; }
        Player ActualPlayer { get; set; }
        GameItem[,] GameMatrix { get; set; }        
        int[] SelectedFigure { get; set; }
        int[] EnemyFigure { get; set; }
        bool Turned { get; set; }
        List<int[]> PossibleMoves { get; set; }
        TimeSpan PlayTime { get; set; }

        event EventHandler GameOver;
        event EventHandler Select;
        event EventHandler<ChangedEventArgs> Changed;
        event EventHandler Next;
        void AnimationEnded();

    }
}