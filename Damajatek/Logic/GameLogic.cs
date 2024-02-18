using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Damajatek.Logic;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Damajatek
{
    public enum GameItem
    {
        lightFigure, darkFigure, darkFloor, lightFloor, lightFigureQueen, darkFigureQueen
    }

    public class ChangedEventArgs : EventArgs
    {
        public int X { get; set; }        
        public int Y { get; set; }        
        public int[] Direction { get; set; }        
        public GameItem Item { get; set; }        
        public int Duration { get; set; }
        public int EnemyFigureX { get; set; }
        public int EnemyFigureY { get; set; }
        public GameItem EnemyFigure { get; set; }
                
        public ChangedEventArgs(int x, int y, int[] direction, GameItem item, int duration)
        {
            X = x;
            Y = y;
            Direction = direction;
            Item = item;
            Duration = duration;
        }

        public ChangedEventArgs(int x, int y, int[] direction, GameItem item, int duration, int enemyFigureX, int enemyFigureY, GameItem enemyFigure) : this(x, y, direction, item, duration)
        {
            EnemyFigureX = enemyFigureX;
            EnemyFigureY = enemyFigureY;
            EnemyFigure = enemyFigure;
        }
        public ChangedEventArgs() : base()
        {
        }
    }

    public class GameLogic : IGameModel, IGameControl
    {
        DispatcherTimer dtNextPlayer;
        static DispatcherTimer dtAIMove;
        public int moveCounter;
        GameItem[,] gameMatrix;
        static readonly Random random = new Random();
        private bool isThereChainedStrike;
        private string winnerName;

        public event EventHandler GameOver;
        public event EventHandler Select;
        public event EventHandler<ChangedEventArgs> Changed;
        public event EventHandler Next;

        public GameItem[,] GameMatrix { get => gameMatrix; set => gameMatrix = value; }
        public Player Light { get; set; }
        public Player Dark { get; set; }
        public Queue<Player> NextPlayer { get; set; }
        public Player ActualPlayer { get; set; }
                
        public int[] SelectedFigure { get; set; }
        public int[] EnemyFigure { get; set; }

        public List<int[]> PossibleMoves { get; set; }
        public TimeSpan PlayTime { get; set; }
        public bool Turned { get; set; }
        public DateTime StartTime { get; private set; }
                
        public GameLogic(string TomakeDifferentConstructor)
        {
            gameMatrix = new GameItem[8, 8];
            NextPlayer = new Queue<Player>();
            this.SelectedFigure = new int[] { -1, -1 };
            this.EnemyFigure = new int[] { -1, -1 };
            moveCounter = 0;
            ActualPlayer = new Player();
            DefaultGameLoad();

            string[] s = File.ReadAllLines("Settings.dat");
            bool aiGame = bool.Parse(s[0].Split('=')[1]);
            string name1 = s[1].Split('=')[1];
            string name2 = s[2].Split('=')[1];
            WhoStart(aiGame, name1, name2);

            dtNextPlayer = new DispatcherTimer();
            dtNextPlayer.Interval = TimeSpan.FromMilliseconds(1000);
            dtNextPlayer.Tick += (sender, eventargs) => Next?.Invoke(this, EventArgs.Empty);
            dtNextPlayer.Tick += (sender, eventargs) => dtNextPlayer.Stop();

            dtAIMove = new DispatcherTimer();
            dtAIMove.Tick += (sender, eventargs) => this.AIMove();
            dtAIMove.Tick += (sender, eventargs) => dtAIMove.Stop();

            if (ActualPlayer.AI) // if AI is with the white figures, meaning AI is first, so the screen nneds to be rotated
            {
                dtAIMove.Interval = TimeSpan.FromMilliseconds(random.Next(3500, 4000));
                dtAIMove.Start();
                dtNextPlayer.Start();
            }
            StartTime = DateTime.Now;
        }
                
        public void AnimationEnded()
        {
            if (ActualPlayer.AI && isThereChainedStrike)
            {
                dtAIMove.Interval = TimeSpan.FromMilliseconds(500);
                dtAIMove.Start();
                isThereChainedStrike = false;
            }
        }

        public void SaveGame(string fileName, bool turn)
        {
            this.Turned = turn;
            string jsonContent = JsonConvert.SerializeObject(this);
            File.WriteAllText(fileName, jsonContent);
        }

        public void SelectFigure(int x, int y)
        {
            if (CoordinateCheck(x, y))
            {
                if (ActualPlayer.Equals(gameMatrix[x, y]))
                {
                    if (IsThereForcedStrike())
                    {
                        if (IsThereForcedStrike(x, y).Length > 0) //ha van ütéskényszeres bábu, akkor csak azt választhatja    
                        {
                            // ütéskényszer és jó bábu van kiválasztva
                            this.SelectedFigure[0] = x;
                            this.SelectedFigure[1] = y;
                            PossibleMoves = this.PossibleMoveSearch(x, y, gameMatrix[x, y]);
                            this.EnemyFigure = PossibleMoves.First();
                        }
                    }
                    else
                    {
                        // nincs ütéskényszeres
                        this.SelectedFigure[0] = x;
                        this.SelectedFigure[1] = y;
                        PossibleMoves = this.PossibleMoveSearch(x, y, gameMatrix[x, y]);
                    }
                    Select?.Invoke(this, EventArgs.Empty);
                }

            }
            //GameOver?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Ensure if the move is valid
        /// </summary>
        /// <param name="x">New Coordinate X</param>
        /// <param name="y">New Coordinate Y</param>
        public void Move(int x, int y)
        {
            if (PossibleMoves == null)
            {
                return;
            }
            if (x >= 0 && y >= 0 && x < gameMatrix.GetLength(0) && y < gameMatrix.GetLength(1))
            {
                foreach (var item in PossibleMoves)
                {
                    // if the selected floor is a possible move
                    if (item[0] == x && item[1] == y)
                    {
                        bool strikeChain = false;
                        moveCounter++;
                        if (gameMatrix[x, y] == GameItem.lightFloor || gameMatrix[x, y] == GameItem.darkFloor)
                        {
                            int[] direction = DirectionVector(SelectedFigure[0], SelectedFigure[1], x, y);
                            ChangedEventArgs args = new ChangedEventArgs(SelectedFigure[0], SelectedFigure[1], direction, gameMatrix[SelectedFigure[0], SelectedFigure[1]], 1);
                            Changed?.Invoke(this, args);
                            dtNextPlayer.Interval = TimeSpan.FromMilliseconds(1000);

                            gameMatrix[x, y] = gameMatrix[SelectedFigure[0], SelectedFigure[1]];
                            gameMatrix[SelectedFigure[0], SelectedFigure[1]] = GameItem.darkFloor;

                            IsQueen(x, y);
                        }
                        else // Enemy figure
                        {
                            int[] direction = DirectionVector(SelectedFigure[0], SelectedFigure[1], x, y); // [-1 -1] [ 1 1] [-1 1] [1 -1]
                            int nextX = x + direction[0];
                            int nextY = y + direction[1];

                            ChangedEventArgs args = new ChangedEventArgs(SelectedFigure[0], SelectedFigure[1], direction, gameMatrix[SelectedFigure[0], SelectedFigure[1]], 2, x, y, gameMatrix[x, y]);
                            Changed?.Invoke(this, args);
                            dtNextPlayer.Interval = TimeSpan.FromMilliseconds(2000); // pálya

                            this.EnemyFigure = new int[] { -1, -1 };

                            GameItem temp = gameMatrix[nextX, nextY];
                            gameMatrix[x, y] = gameMatrix[SelectedFigure[0], SelectedFigure[1]];
                            gameMatrix[SelectedFigure[0], SelectedFigure[1]] = temp;

                            gameMatrix[nextX, nextY] = gameMatrix[x, y];
                            gameMatrix[x, y] = temp;

                            IsQueen(nextX, nextY);

                            foreach (var moves in PossibleMoveSearch(nextX, nextY, gameMatrix[nextX, nextY]))
                            {
                                if (IsEnemyFigure(gameMatrix[nextX, nextY], gameMatrix[moves[0], moves[1]]))
                                {
                                    strikeChain = true;
                                }
                            }

                            if (strikeChain)
                            {
                                PossibleMoves = PossibleMoveSearch(nextX, nextY, gameMatrix[nextX, nextY]);
                                SelectedFigure[0] = nextX;
                                SelectedFigure[1] = nextY;
                            }
                        }
                        if (!IsGameOver())
                        {
                            if (!strikeChain)
                            {
                                // lépés vége dolgok kinullázása
                                this.NextPlayer.Enqueue(ActualPlayer);
                                ActualPlayer = NextPlayer.Dequeue();
                                SelectedFigure = new int[] { -1, -1 };
                                EnemyFigure = new int[] { -1, -1 };
                                PossibleMoves = new List<int[]>();
                            }

                            if (!strikeChain && !Dark.AI && !Light.AI) // pálya forgatása
                            {
                                dtNextPlayer.Start(); // Next fire event //Next?.Invoke(this, EventArgs.Empty);
                            }
                            if (!strikeChain && (Dark.AI || Light.AI)) // AI lép
                            {
                                dtAIMove.Interval = TimeSpan.FromMilliseconds(random.Next(1500, 3000));
                                dtAIMove.Start();
                            }

                            IsGameOver();
                        }
                    }
                }
            }


        }

        /// <summary>
        /// Return the coordinates where the selected figure can move
        /// </summary>
        /// <param name="selected_i"></param>
        /// <param name="selected_j"></param>
        /// <param name="selectedFigure"></param>
        /// <returns></returns>
        private List<int[]> PossibleMoveSearch(int selected_i, int selected_j, GameItem selectedFigure)
        {
            List<int[]> result = new List<int[]>();

            if (CanMoveHere(selected_i - 1, selected_j - 1, selected_i, selected_j, selectedFigure))
            {
                result.Add(new int[] { selected_i - 1, selected_j - 1 });
            }
            if (CanMoveHere(selected_i - 1, selected_j + 1, selected_i, selected_j, selectedFigure))
            {
                result.Add(new int[] { selected_i - 1, selected_j + 1 });
            }
            if (CanMoveHere(selected_i + 1, selected_j - 1, selected_i, selected_j, selectedFigure))
            {
                result.Add(new int[] { selected_i + 1, selected_j - 1 });
            }
            if (CanMoveHere(selected_i + 1, selected_j + 1, selected_i, selected_j, selectedFigure))
            {
                result.Add(new int[] { selected_i + 1, selected_j + 1 });
            }


            bool enemyOnSight = false;
            foreach (var item in result)
            {
                if (IsEnemyFigure(gameMatrix[item[0], item[1]], selectedFigure))
                {
                    enemyOnSight = true;
                }
            }
            // if there is at least one Enemy, then we only need the enemies
            if (enemyOnSight)
            {
                List<int[]> result2 = new List<int[]>();
                foreach (var item in result)
                {
                    if (IsEnemyFigure(gameMatrix[item[0], item[1]], selectedFigure))
                    {
                        result2.Add(item);
                    }
                }
                result = result2;
            }
            // else nothing happens all good

            //PossibleMoves = result;
            return result;
        }

        private int[] DirectionVector(int StartX, int StartY, int EndX, int EndY)
        {
            int[] vector = new int[2];
            vector[0] = (EndX - StartX);
            vector[1] = (EndY - StartY);
            return vector;
        }

        private bool CoordinateCheck(int x, int y)
        {
            return x >= 0 && x < 8 && y >= 0 && y < 8;
        }

        private bool CanMoveHere(int newX, int newY, int selected_i, int selected_j, GameItem selectedFigure)
        {
            if (CoordinateCheck(newX, newY))    // a lépés a pályán belül van-e
            {
                if (!SameTeam(selectedFigure, gameMatrix[newX, newY]))      // csapattársat nem üthet
                {
                    int[] direction = DirectionVector(selected_i, selected_j, newX, newY); // [-1 -1] [ 1 1] [-1 1] [1 -1]
                    int következőMezőX = newX + direction[0];
                    int következőMezőY = newY + direction[1];

                    // ha enemy bábura lépnénk
                    if (gameMatrix[newX, newY] != GameItem.lightFloor && gameMatrix[newX, newY] != GameItem.darkFloor)
                    {
                        // ha enemy bábura lépnénk, akkor mögötte üres helynek kell lennie, különben nem léphetünk oda
                        if (CoordinateCheck(következőMezőX, következőMezőY)
                            && (gameMatrix[következőMezőX, következőMezőY] == GameItem.darkFloor || gameMatrix[következőMezőX, következőMezőY] == GameItem.lightFloor))
                        {
                            if (selectedFigure == GameItem.lightFigure && direction[0] == -1)
                            {
                                return true;
                            }
                            else if (selectedFigure == GameItem.darkFigure && direction[0] == 1)
                            {
                                return true;
                            }
                            else if (selectedFigure == GameItem.lightFigureQueen || selectedFigure == GameItem.darkFigureQueen)
                            {
                                return true; // Queen
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    // üres mező
                    else
                    {
                        if (selectedFigure == GameItem.lightFigure && direction[0] == -1)
                        {
                            return true;
                        }
                        else if (selectedFigure == GameItem.darkFigure && direction[0] == 1)
                        {
                            return true;
                        }
                        else if (selectedFigure == GameItem.lightFigureQueen || selectedFigure == GameItem.darkFigureQueen)
                        {
                            return true; // Queen
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        private bool SameTeam(GameItem item1, GameItem item2)
        {
            if (item1 == item2)
            {
                return true;
            }
            else if (item1.ToString().Contains(item2.ToString()))
            {
                return true;
            }
            else if (item2.ToString().Contains(item1.ToString()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns if the two figure are in different team
        /// </summary>
        /// <param name="item"></param>
        /// <param name="selectedFigure"></param>
        /// <returns></returns>
        private bool IsEnemyFigure(GameItem item, GameItem selectedFigure)
        {
            if (item == GameItem.darkFloor || item == GameItem.lightFloor || selectedFigure == GameItem.darkFloor || selectedFigure == GameItem.lightFloor)
            {
                return false;
            }
            return !item.ToString().Contains(selectedFigure.ToString());
        }

        private void WhoStart(bool aiGame, string name1, string name2)
        {
            if (!aiGame)
            {
                if (random.Next(0, 10) < 5)
                {
                    Light = new Player(aiGame, GameItem.lightFigure, name1);
                    Dark = new Player(aiGame, GameItem.darkFigure, name2);
                }
                else
                {
                    Light = new Player(aiGame, GameItem.lightFigure, name2);
                    Dark = new Player(aiGame, GameItem.darkFigure, name1);
                }
            }
            else if (random.Next(0, 10) < 5)
            {
                Light = new Player(false, GameItem.lightFigure, name1);
                Dark = new Player(aiGame, GameItem.darkFigure, "AI");
            }
            else
            {
                Light = new Player(aiGame, GameItem.lightFigure, "AI");
                Dark = new Player(false, GameItem.darkFigure, name1);
            }

            NextPlayer.Enqueue(Light);
            NextPlayer.Enqueue(Dark);
            ActualPlayer = NextPlayer.Dequeue();
        }

        private void DefaultGameLoad()
        {
            for (int i = 0; i < gameMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < gameMatrix.GetLength(1); j++)
                {
                    if (i % 2 == 0 && j % 2 == 0) // 0,0 0,2 0,4 .. 2,0 2,2 2,4 
                    {
                        if (i < 3)
                        {
                            gameMatrix[i, j] = GameItem.darkFigure;
                        }
                        else if (i > 4)
                        {
                            gameMatrix[i, j] = GameItem.lightFigure;
                        }
                        else
                        {
                            gameMatrix[i, j] = GameItem.darkFloor;
                        }
                    }
                    else if (i % 2 == 1 && j % 2 == 1) // 1,1 1,3 1,5
                    {
                        if (i < 3)
                        {
                            gameMatrix[i, j] = GameItem.darkFigure;
                        }
                        else if (i > 4)
                        {
                            gameMatrix[i, j] = GameItem.lightFigure;
                        }
                        else
                        {
                            gameMatrix[i, j] = GameItem.darkFloor;
                        }
                    }
                    else
                    {
                        gameMatrix[i, j] = GameItem.lightFloor;
                    }
                }
            }
        }

        /// <summary>
        /// A paraméterként megadott bábu, ütéskényszeres-e és hová kell lépnie
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int[] IsThereForcedStrike(int x, int y)
        {
            for (int i = 0; i < gameMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < gameMatrix.GetLength(1); j++)
                {
                    if (SameTeam(gameMatrix[i, j], ActualPlayer.Figure))
                    {
                        foreach (var moves in PossibleMoveSearch(i, j, gameMatrix[i, j]))
                        {
                            if (IsEnemyFigure(gameMatrix[i, j], gameMatrix[moves[0], moves[1]]))
                            {
                                if (i == x && j == y)
                                {
                                    return new int[2] { moves[0], moves[1] };
                                }
                            }
                        }
                    }
                }
            }
            return new int[0];
        }

        /// <summary>
        /// Returns if there is any forced strike figure
        /// </summary>
        /// <returns></returns>
        private bool IsThereForcedStrike()
        {
            for (int i = 0; i < gameMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < gameMatrix.GetLength(1); j++)
                {
                    if (SameTeam(gameMatrix[i, j], ActualPlayer.Figure))
                    {
                        foreach (var moves in PossibleMoveSearch(i, j, gameMatrix[i, j]))
                        {
                            if (IsEnemyFigure(gameMatrix[i, j], gameMatrix[moves[0], moves[1]]))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }


        private bool AIMove()
        {
            isThereChainedStrike = false;
            if (IsThereForcedStrike())
            {
                for (int i = 0; i < gameMatrix.GetLength(0); i++)
                {
                    for (int j = 0; j < gameMatrix.GetLength(1); j++)
                    {
                        if (SameTeam(gameMatrix[i, j], ActualPlayer.Figure))
                        {
                            int[] target = IsThereForcedStrike(i, j); // Ütéskényszeres bábu?
                            if (target.Length > 0)
                            {
                                // ütéskényszeres és azt is tudjuk hol van az ellen
                                int[] direction = DirectionVector(i, j, target[0], target[1]); // [-1 -1] [ 1 1] [-1 1] [1 -1]
                                int nextX = target[0] + direction[0];
                                int nextY = target[1] + direction[1];

                                ChangedEventArgs args = new ChangedEventArgs(i, j, direction, gameMatrix[i, j], 2, target[0], target[1], gameMatrix[target[0], target[1]]);
                                Changed?.Invoke(this, args);

                                GameItem temp = gameMatrix[nextX, nextY];
                                gameMatrix[target[0], target[1]] = gameMatrix[i, j];
                                gameMatrix[i, j] = temp;

                                gameMatrix[nextX, nextY] = gameMatrix[target[0], target[1]];
                                gameMatrix[target[0], target[1]] = temp;

                                IsQueen(nextX, nextY);


                                foreach (var moves in PossibleMoveSearch(nextX, nextY, gameMatrix[nextX, nextY]))
                                {
                                    if (IsEnemyFigure(gameMatrix[nextX, nextY], gameMatrix[moves[0], moves[1]]))
                                    {
                                        //dtAIMove.Interval = TimeSpan.FromMilliseconds(random.Next(1500));
                                        //dtAIMove.Start();
                                        //Thread.Sleep(3000);
                                        //ChainedStrike?.Invoke(this, EventArgs.Empty);
                                        isThereChainedStrike = true;
                                        i = gameMatrix.GetLength(0);
                                        j = gameMatrix.GetLength(1);
                                    }
                                }


                            }
                        }

                    }
                }

            }
            else //ha nincs ütéskényszer akkor 1 random bábuval tesz 1 érvényes lépést
            {
                List<List<int[]>> PossibleFiguresAndMoves = new List<List<int[]>>();
                List<int[]> PossibleFigures = new List<int[]>();
                for (int i = 0; i < gameMatrix.GetLength(0); i++)
                {
                    for (int j = 0; j < gameMatrix.GetLength(1); j++)
                    {
                        if (SameTeam(gameMatrix[i, j], ActualPlayer.Figure))
                        {
                            List<int[]> possiblemove1figure = PossibleMoveSearch(i, j, gameMatrix[i, j]);
                            if (possiblemove1figure.Count > 0)
                            {
                                PossibleFiguresAndMoves.Add(possiblemove1figure);
                                PossibleFigures.Add(new int[2] { i, j });
                            }
                        }
                    }
                }
                // kiválasztunk egy random bábut és annak egy random lépését
                int r = random.Next(0, PossibleFiguresAndMoves.Count);
                int seged = PossibleFiguresAndMoves[r].Count;
                int[] move = PossibleFiguresAndMoves[r][random.Next(0, seged)];
                int[] SelectedFigure = PossibleFigures[r];

                // megtesszük a lépést
                int[] direction = DirectionVector(SelectedFigure[0], SelectedFigure[1], move[0], move[1]);
                ChangedEventArgs args = new ChangedEventArgs(SelectedFigure[0], SelectedFigure[1], direction, gameMatrix[SelectedFigure[0], SelectedFigure[1]], 1);
                Changed?.Invoke(this, args);
                dtNextPlayer.Interval = TimeSpan.FromMilliseconds(1000);

                gameMatrix[move[0], move[1]] = gameMatrix[SelectedFigure[0], SelectedFigure[1]];
                gameMatrix[SelectedFigure[0], SelectedFigure[1]] = GameItem.darkFloor;

                IsQueen(move[0], move[1]);

            }

            // if there is chained strike then we don't change player
            if (!isThereChainedStrike)
            {
                this.NextPlayer.Enqueue(ActualPlayer);
                ActualPlayer = NextPlayer.Dequeue();
            }
            return isThereChainedStrike;
        }

        private bool IsGameOver()
        {
            int darkCounter = 0;
            int lightCounter = 0;
            List<int[]> possibleMovesLight = new List<int[]>();
            List<int[]> possibleMovesDark = new List<int[]>();

            for (int i = 0; i < gameMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < gameMatrix.GetLength(1); j++)
                {
                    if (gameMatrix[i, j] == GameItem.darkFigureQueen || gameMatrix[i, j] == GameItem.darkFigure)
                    {
                        darkCounter++;
                        List<int[]> possibleMoves = PossibleMoveSearch(i, j, gameMatrix[i, j]);
                        if (possibleMoves.Count > 0)
                        {
                            possibleMovesDark.AddRange(possibleMoves);
                            // if at least 1 figure can move, than the game is not over
                        }
                    }
                    if (gameMatrix[i, j] == GameItem.lightFigureQueen || gameMatrix[i, j] == GameItem.lightFigure)
                    {
                        lightCounter++;
                        List<int[]> possibleMoves = PossibleMoveSearch(i, j, gameMatrix[i, j]);
                        if (possibleMoves.Count > 0)
                        {
                            possibleMovesLight.AddRange(possibleMoves);
                            // if at least 1 figure can move, than the game is not over
                        }
                    }
                }
            }
            if (darkCounter == 0 || possibleMovesDark.Count == 0 || lightCounter == 0 || possibleMovesLight.Count == 0)
            {
                if (darkCounter == 0 || possibleMovesDark.Count == 0) // Dark Loose
                {
                    Dark.IsLooser = true;
                    Light.IsWinner = true;
                    winnerName = Light.Name;
                }
                else if (lightCounter == 0 || possibleMovesLight.Count == 0) // Light Loose
                {
                    Dark.IsWinner = true;
                    Light.IsLooser = true;
                    winnerName = Dark.Name;
                }
                else // Draw
                {
                    Dark.IsLooser = true;
                    Light.IsLooser = true;
                    winnerName = "It is a Draw!";
                }
                PlayTime = DateTime.Now - StartTime;
                if (!Light.AI && !Dark.AI) // if its a human vs human, than moveCountert needs to be halfed
                {
                    moveCounter = moveCounter >> 1;
                }
                //Score = (int)PlayTime.Ticks / moveCounter;
                SaveStatistic();
                GameOver?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private void IsQueen(int x, int y)
        {
            if (x == 0 || x == gameMatrix.GetLength(0) - 1)
            {
                if (x == 0 && gameMatrix[x, y] == GameItem.lightFigure)
                {
                    gameMatrix[x, y] = GameItem.lightFigureQueen;
                }
                if (x == gameMatrix.GetLength(0) - 1 && gameMatrix[x, y] == GameItem.darkFigure)
                {
                    gameMatrix[x, y] = GameItem.darkFigureQueen;
                }
            }
        }

        private void SaveStatistic()
        {
            string s = Light.Name + " VS " + Dark.Name + " The Winner is: " + winnerName + " Step count: " + moveCounter + " Time: " + PlayTime.ToString(@"hh\:mm\:ss") + " Date of game: " + DateTime.Now.ToString() + "\n";
            File.AppendAllText(Directory.GetCurrentDirectory() + "\\Statistics.dat", s);
        }


    }
}
