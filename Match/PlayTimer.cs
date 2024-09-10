namespace NMX.EzChess.Library.Match
{
    public sealed class PlayTimer
    {
        public int TotalTimeLimit { get; private set; }
        public int MoveTimeLimit { get; private set; }
        public bool HasTotalTimer => TotalTimer > 0;
        public bool HasMoveTimer => MoveTimer > 0;
        public int TotalTimer { get; private set; }
        public int MoveTimer { get; private set; }

        internal PlayTimer CreateDeepClone() => new PlayTimer()
        {
            TotalTimeLimit = TotalTimeLimit,
            MoveTimeLimit = MoveTimeLimit,
            TotalTimer = TotalTimer,
            MoveTimer = MoveTimer,
        };

        internal void Set(in int p_totalTimeLimit, in int p_moveTimeLimit)
        {
            TotalTimer = TotalTimeLimit = p_totalTimeLimit;
            MoveTimer = MoveTimeLimit = p_moveTimeLimit;
        }

        internal bool DecrementTimers(in int p_deduction)
        {
            bool _timeout = false;
            if (HasTotalTimer)
            {
                TotalTimer -= p_deduction;
                if (TotalTimer <= 0) _timeout = true;
            }
            if (HasMoveTimer)
            {
                MoveTimer -= p_deduction;
                if (MoveTimer <= 0) _timeout = true;
            }
            return _timeout;
        }

        internal void ResetMoveTimer()
        {
            MoveTimer = TotalTimer < MoveTimeLimit ? TotalTimer : MoveTimeLimit;
        }
    }
}
