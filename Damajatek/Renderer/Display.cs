using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Damajatek.Renderer
{
    public class Display : FrameworkElement
    {
        Size size;
        IGameModel model;
        ImageBrush boardBrush;
        ImageBrush darkFigure;
        ImageBrush lightFigure;
        ImageBrush lightQueenFigure;
        ImageBrush darkQueenFigure;
        ImageBrush darkFloor;
        ImageBrush lightFloor;
        ImageBrush borderBrush;
        ImageBrush AnimatedFigureBrush;
        CultureInfo culture;
        DispatcherTimer dt;
        DispatcherTimer dt2;

        double tileWidth;
        double tileHeight;

        Rect mouse;

        public double TileWidth { get => tileWidth; set => tileWidth = value; }
        public double TileHeight { get => tileHeight; set => tileHeight = value; }

        double angle;
        double dinamicAngle;
        bool turnedUpsideDown;
        bool animateTurn;
        bool animateMoveFigure;
        bool isGameOver;
        bool firstMove;
        double figureStartX, figureStartY, figureDeltaX, figureDeltaY, figureEndX, figureEndY;
        int Duration;
        double[] direction;
        GameItem enemyFigure;
        int enemyFigureX;
        int enemyFigureY;
        int countX = 0;

        public bool TurnedUpsideDown { get => turnedUpsideDown; set => turnedUpsideDown = value; }
        public bool AnimateTurn { get => animateTurn; set => animateTurn = value; }
        public bool AnimateMoveFigure { get => animateMoveFigure; set => animateMoveFigure = value; }
        public bool IsGameOver { get => isGameOver; set => isGameOver = value; }
        public double Angle { get => angle; set => angle = value; }

        public void Init(IGameModel model)
        {
            this.model = model;
            culture = CultureInfo.CurrentCulture;
            mouse = new Rect(-1, -1, 0.5, 0.5);
            this.model.Select += (sender, eventargs) => this.InvalidateVisual();
            this.model.Select += (sender, eventargs) => this.FirstMove();
            this.model.Next += (sender, eventargs) => this.Turn180Degree();
            this.model.Changed += (sender, eventargs) => this.MoveFigure(eventargs);
            this.model.GameOver += (sender, eventargs) => this.GameOver();

            isGameOver = false;
            firstMove = true;
            IsEnabled = true;

            figureStartX = -10;
            figureStartY = -10;
            turnedUpsideDown = false;
            AnimateTurn = false;
            Angle = 0;

            // Board brushes
            boardBrush = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "keretbetukkel.png"), UriKind.RelativeOrAbsolute)));

            // darkFigure brush
            darkFigure = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "feketebabu.png"), UriKind.RelativeOrAbsolute)
                ));

            // darkQueen brush
            darkQueenFigure = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "feketebabudama.png"), UriKind.RelativeOrAbsolute)
                ));

            // lightFigure brush
            lightFigure = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "pirosbabu.png"), UriKind.RelativeOrAbsolute)
                ));

            // lightQueen brush
            lightQueenFigure = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "pirosbabudama.png"), UriKind.RelativeOrAbsolute)
                ));

            // lightFloor Brush
            darkFloor = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "darkFloor.png"), UriKind.RelativeOrAbsolute)
                ));

            // darkFloor Brush
            lightFloor = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "lightFloor.png"), UriKind.RelativeOrAbsolute)
                ));

            //Border Brush
            borderBrush = new ImageBrush(
                new BitmapImage(new Uri
                (Path.Combine("Images", "kivaasztokeret.png"), UriKind.RelativeOrAbsolute)
                ));
        }

        private void FirstMove()
        {
            firstMove = false;
            this.model.Select -= (sender, eventargs) => FirstMove();
        }

        private void GameOver()
        {
            this.IsEnabled = false;
            IsGameOver = true;
            Task task = new Task(() => PlaySound(Path.Combine("Sound", "game_over.mp3")));
            task.Start();
        }

        private void MoveFigure(ChangedEventArgs e)
        {
            if (e == null)
            {
                return;
            }
            switch (e.Item)
            {
                case GameItem.lightFigure:
                    AnimatedFigureBrush = lightFigure;
                    break;
                case GameItem.darkFigure:
                    AnimatedFigureBrush = darkFigure;
                    break;
                case GameItem.lightFigureQueen:
                    AnimatedFigureBrush = lightQueenFigure;
                    break;
                case GameItem.darkFigureQueen:
                    AnimatedFigureBrush = darkQueenFigure;
                    break;
                default:
                    break;
            }
            direction = new double[2];
            direction[0] = e.Direction[1]; // X == 0 => X == Width => X == j 
            direction[1] = e.Direction[0]; // Y == 1 => Y == Height => Y == i
            int directionsegedX = e.Direction[0];
            int directionsegedY = e.Direction[1];
            figureStartX = e.X;
            figureStartY = e.Y;
            figureDeltaX = figureStartX;
            figureDeltaY = figureStartY;
            Duration = e.Duration;
            figureEndX = figureStartX + direction[0] * Duration;
            figureEndY = figureStartY + direction[1] * Duration;
            direction[0] = ((figureEndX * tileWidth + tileWidth + tileWidth / 2) - (figureStartX * tileWidth + tileWidth + tileWidth / 2)) / 100; //End - Start
            direction[1] = ((figureEndY * tileHeight + tileHeight + tileHeight / 2) - (figureStartY * tileHeight + tileHeight + tileHeight / 2)) / 100; // (j * tileWidth + tileWidth + tileWidth / 2, i * tileHeight + tileHeight + tileHeight / 2);
            AnimateMoveFigure = true;

            figureEndX = figureStartX + directionsegedX * Duration;
            figureEndY = figureStartY + directionsegedY * Duration;
            enemyFigure = e.EnemyFigure;
            enemyFigureX = model.EnemyFigure[0];
            enemyFigureY = model.EnemyFigure[1];

            dt2 = new DispatcherTimer();
            dt2.Interval = TimeSpan.FromMilliseconds(1);
            dt2.Tick += this.Dt_Tick_MoveFigure;
            dt2.Start();

        }


        private void Dt_Tick_MoveFigure(object? sender, EventArgs e)
        {
            figureDeltaX += direction[0] * 3 / Duration;    // X == 0 => X == Width => X == j 
            figureDeltaY += direction[1] * 3 / Duration;    // Y == 1 => Y == Height => Y == i
            double seged = Math.Abs(direction[0] * 3 / Duration);
            double seged2 = Math.Abs(direction[1] * 3 / Duration);
            if (Duration > 1 && ((Math.Abs(figureDeltaX) >= tileWidth - seged && Math.Abs(figureDeltaX) <= tileWidth) || (Math.Abs(figureDeltaY) >= tileHeight - seged2 && Math.Abs(figureDeltaY) <= tileHeight)))
            {
                if (countX < 1)
                {
                    dt2.Interval = TimeSpan.FromMilliseconds(200);
                    countX++;
                }
                else
                {
                    dt2.Interval = TimeSpan.FromMilliseconds(1);
                }
            }
            else if (Duration > 1)
            {
                dt2.Interval = TimeSpan.FromMilliseconds(1);
            }

            this.InvalidateVisual();

            if (Math.Abs(figureDeltaX) >= tileWidth * Duration || Math.Abs(figureDeltaY) >= TileHeight * Duration)//figureDeltaX * tileHeight == (figureX + direction[1]) * tileHeight && figureDeltaY * tileWidth == (figureY + direction[0]) * tileWidth)
            {
                dt2.Stop();
                dt2 = new DispatcherTimer();
                AnimateMoveFigure = false;
                figureStartX = -10;
                figureStartY = -10;
                countX = 0;

                enemyFigureX = -100;
                enemyFigureY = -100;
                model.AnimationEnded();

                Task task = Task.Run(() => PlaySound(Path.Combine("Sound", "move.mp3")));
                task.Wait(1000);
            }
        }
        private static void PlaySound(string path)
        {
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri(path, UriKind.RelativeOrAbsolute));
            mediaPlayer.Play();
        }

        public void Resize(Size size)
        {

            this.size = size;
            this.InvalidateVisual();

        }

        public void MouseMov(double mouseX, double mouseY)
        {
            if (!this.IsGameOver)
            {
                mouse.X = mouseX;
                mouse.Y = mouseY;
                this.InvalidateVisual();
            }
        }

        private void Turn180Degree()
        {
            AnimateTurn = true;
            dinamicAngle = Angle;
            Angle += 180;
            turnedUpsideDown = !turnedUpsideDown;
            mouse.X = 0;
            mouse.Y = 0;


            dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromMilliseconds(1);
            dt.Tick += this.Dt_Tick;
            dt.Start();
        }

        private void Dt_Tick(object? sender, EventArgs e)
        {
            dinamicAngle += 1 * 3;
            this.InvalidateVisual();
            if (dt != null && dinamicAngle >= Angle)
            {
                dt.Stop();
                AnimateTurn = false;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (model == null || Math.Abs(size.Height - size.Width) > 1000)
            {
                return;
            }

            tileWidth = size.Width / (model.GameMatrix.GetLength(1) + 2);
            tileHeight = size.Height / (model.GameMatrix.GetLength(0) + 2);

            drawingContext.DrawRectangle(Brushes.Beige, new Pen(Brushes.Beige, 0), new Rect(0, 0, size.Width, size.Height));


            if (AnimateTurn)
            {
                drawingContext.PushTransform(new RotateTransform(dinamicAngle, size.Width / 2, size.Height / 2));
                drawingContext.PushTransform(new ScaleTransform(0.98, 0.95, size.Width / 2, size.Height / 2));
            }
            if (turnedUpsideDown && !AnimateTurn)
            {
                drawingContext.PushTransform(new RotateTransform(Angle, size.Width / 2, size.Height / 2));
            }

            drawingContext.DrawRectangle(boardBrush, new Pen(Brushes.Black, 0), new Rect(0, 0, size.Width, size.Height));

            bool hasBorder = false;

            for (int x = 0; x < model.GameMatrix.GetLength(0); x++)
            {
                for (int y = 0; y < model.GameMatrix.GetLength(1); y++)
                {
                    ImageBrush brush = new ImageBrush();

                    switch (model.GameMatrix[x, y])
                    {
                        case GameItem.lightFigure:
                            brush = lightFigure;
                            break;
                        case GameItem.darkFigure:
                            brush = darkFigure;
                            break;
                        case GameItem.darkFloor:
                            brush = darkFloor;
                            break;
                        case GameItem.lightFloor:
                            brush = lightFloor;
                            break;
                        case GameItem.lightFigureQueen:
                            brush = lightQueenFigure;
                            break;
                        case GameItem.darkFigureQueen:
                            brush = darkQueenFigure;
                            break;
                        default:
                            break;
                    }

                    Rect floor = new Rect(tileWidth + y * tileWidth, tileHeight + x * tileHeight, tileWidth, tileHeight);

                    if (model.GameMatrix[x, y] is GameItem.lightFloor || model.GameMatrix[x, y] is GameItem.darkFloor)
                    {
                        drawingContext.DrawRectangle(brush, new Pen(Brushes.Black, 0), floor);
                    }
                    else
                    {
                        drawingContext.DrawRectangle(darkFloor, new Pen(Brushes.Black, 0), floor);
                        Point center = new Point(y * tileWidth + tileWidth + tileWidth / 2, x * tileHeight + tileHeight + tileHeight / 2);

                        if (model.SelectedFigure[0] == x && model.SelectedFigure[1] == y)
                        {
                            //drawingContext.DrawEllipse(brush, new Pen(Brushes.Wheat, 3), new Point(j * tileWidth + tileWidth + tileWidth / 2, i * tileHeight + tileHeight + tileHeight / 2), tileWidth / 2, tileHeight / 2);
                            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(90, 245, 225, 179)), new Pen(Brushes.Black, 0), floor);
                        }

                        if (AnimateMoveFigure)
                        {
                            if (x == figureEndX && y == figureEndY) // if the actual figure movement is animated then we don't draw it
                            {
                            }
                            else
                            {
                                drawingContext.DrawEllipse(brush, new Pen(Brushes.Black, 0), center, tileWidth / 2, tileHeight / 2);
                            }
                        }
                        else
                        {
                            drawingContext.DrawEllipse(brush, new Pen(Brushes.Black, 0), center, tileWidth / 2, tileHeight / 2);
                        }
                    }

                    if (!hasBorder && floor.IntersectsWith(mouse))
                    {
                        drawingContext.DrawRectangle(borderBrush, new Pen(Brushes.Black, 0), floor);
                        hasBorder = true;
                    }
                }
            }

            if (model.PossibleMoves != null)
            {
                foreach (var item in model.PossibleMoves)
                {
                    int i = item[0];
                    int j = item[1];
                    Rect floor = new Rect(tileWidth + j * tileWidth, tileHeight + i * tileHeight, tileWidth, tileHeight);
                    Brush brush = new SolidColorBrush(Color.FromArgb(90, 255, 182, 193));
                    if (model.EnemyFigure[0] == i && model.EnemyFigure[1] == j)
                    {
                        brush = new SolidColorBrush(Color.FromArgb(90, 255, 0, 0));
                    }
                    drawingContext.DrawRectangle(brush, new Pen(Brushes.DarkRed, 0), floor);
                }
            }



            if (AnimateMoveFigure)
            {
                if (enemyFigureX > 0 && enemyFigureY > 0 && Math.Abs(figureDeltaX) <= tileWidth)//figureDeltaX , figureDeltaY)
                {
                    ImageBrush brush = new ImageBrush();
                    switch (enemyFigure)
                    //switch (model.GameMatrix[model.EnemyFigure[0], model.EnemyFigure[1]])
                    {
                        case GameItem.lightFigure:
                            brush = lightFigure;
                            break;
                        case GameItem.darkFigure:
                            brush = darkFigure;
                            break;
                        case GameItem.lightFigureQueen:
                            brush = lightQueenFigure;
                            break;
                        case GameItem.darkFigureQueen:
                            brush = darkQueenFigure;
                            break;
                        default:
                            break;
                    }
                    Point center = new Point(enemyFigureY * tileWidth + tileWidth + tileWidth / 2, enemyFigureX * tileHeight + tileHeight + tileHeight / 2);
                    drawingContext.DrawEllipse(brush, new Pen(Brushes.Black, 0), center, tileWidth / 2, tileHeight / 2);
                }

                for (int x = 0; x < model.GameMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < model.GameMatrix.GetLength(1); y++)
                    {
                        if (x == figureStartX && y == figureStartY)
                        {
                            Point center = new Point(y * tileWidth + tileWidth + tileWidth / 2, x * tileHeight + tileHeight + tileHeight / 2);
                            drawingContext.PushTransform(new TranslateTransform(figureDeltaX, figureDeltaY));
                            drawingContext.DrawEllipse(AnimatedFigureBrush, new Pen(Brushes.Black, 0), center, tileWidth / 2, tileHeight / 2);
                            drawingContext.Pop();
                        }
                    }
                }
            }

            if (turnedUpsideDown || AnimateTurn)
            {
                drawingContext.Pop();
            }

            if (firstMove && !AnimateTurn)
            {
                string text;
                if (model.ActualPlayer.AI)
                {
                    text = "Computer is first!";
                }
                else
                {
                    text = model.ActualPlayer.Name + " is first!";
                }

                Point center = new Point(size.Width / 2 - (text.Length / 2 * 24), size.Height / 2 - 24);
                drawingContext.DrawText(new FormattedText(text, culture, FlowDirection.LeftToRight, new Typeface("Arial"), 48, Brushes.White, 2), center);
            }

            if (IsGameOver)
            {
                string text = string.Empty;
                if (model.Dark.AI || model.Light.AI)
                {
                    if (!model.Dark.AI && model.Dark.IsWinner)
                    {
                        text = "Congratulations! The winner is: " + model.Dark.Name;
                    }
                    else if (!model.Light.AI && model.Light.IsWinner)
                    {
                        text = "Congratulations! The winner is: " + model.Light.Name;
                    }
                    else
                    {
                        text = "Loooser.";
                    }
                }
                else
                {
                    if (model.Dark.IsWinner)
                    {
                        text = "Congratulations! The winner is: " + model.Dark.Name;
                    }
                    else if (model.Light.IsWinner)
                    {
                        text = "Congratulations! The winner is: " + model.Light.Name;
                    }
                    else
                    {
                        text = "It's a draw.";
                    }
                }
                Point center = new Point(size.Width / 2 - (text.Length / 2 * 24), size.Height / 2 - 24);
                drawingContext.DrawText(new FormattedText(text, culture, FlowDirection.LeftToRight, new Typeface("Arial"), 48, Brushes.White, 2), center);
            }
        }
    }
}
