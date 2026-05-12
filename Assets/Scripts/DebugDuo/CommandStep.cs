namespace DebugDuo
{
    /// <summary>
    /// One entry in a player's command sequence: which command, and how many times
    /// to repeat it before advancing to the next entry. RepeatCount is always >= 1
    /// (a non-looping block stores 1).
    /// </summary>
    public struct CommandStep
    {
        public CommandType Type;
        public int RepeatCount;

        public CommandStep(CommandType type, int repeatCount)
        {
            Type = type;
            RepeatCount = repeatCount < 1 ? 1 : repeatCount;
        }
    }
}
