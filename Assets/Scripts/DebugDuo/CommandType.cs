namespace DebugDuo
{
    public enum CommandType
    {
        Walk,
        Run,
        TurnLeft,
        TurnRight,
        Wait,
        PressButton,
        WalkBack
    }

    public static class CommandTypeExtensions
    {
        public static string DisplayName(this CommandType c)
        {
            switch (c)
            {
                case CommandType.Walk:        return "Walk";
                case CommandType.Run:         return "Run";
                case CommandType.TurnLeft:    return "Turn Left";
                case CommandType.TurnRight:   return "Turn Right";
                case CommandType.Wait:        return "Wait";
                case CommandType.PressButton: return "Press Button";
                case CommandType.WalkBack:    return "Walk Back";
            }
            return c.ToString();
        }
    }
}
