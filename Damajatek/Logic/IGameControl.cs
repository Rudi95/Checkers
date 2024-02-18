namespace Damajatek
{
    public interface IGameControl
    {
        public void SelectFigure(int i, int j);

        public void Move(int i, int j);

        public void SaveGame(string fileName, bool turned);
    }
}