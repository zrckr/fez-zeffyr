namespace Zeffyr.Structure
{
    public enum Direction
    {
        Left,
        Down,
        Backward,
        Right,
        Up,
        Forward,
        None
    }

    public static class DirExtensions
    {
        public static Direction GetOpposite(this Direction direction)
        {
            return (Direction) ((int) (direction + 3) % 6);
        }

        public static int Sign(this Direction direction)
        {
            if (direction == Direction.None) return 0;
            return (direction >= Direction.Right) ? 1 : -1;
        }

        public static Direction GetDirectionFrom(this float value)
        {
            if (value > 0f) return Direction.Right;
            return (value < 0f) ? Direction.Left : Direction.None;
        }
    }
}