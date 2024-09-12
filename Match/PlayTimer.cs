namespace NMX.EzChess.Library.Match
{
    public sealed class PlayTimer
    {
        public int TotalTimeLimit { get; private set; }
        public int MoveTimeLimit { get; private set; }
        public bool HasTotalTimeLimit => TotalTimeLimit > 0;
        public bool HasMoveTimeLimit => MoveTimeLimit > 0;
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

        internal bool DecrementTimer(in int p_deduction)
        {
            bool _timeout = false;
            if (HasTotalTimeLimit)
            {
                TotalTimer -= p_deduction;
                if (TotalTimer <= 0) _timeout = true;
            }
            if (HasMoveTimeLimit)
            {
                MoveTimer -= p_deduction;
                if (MoveTimer <= 0) _timeout = true;
            }
            return _timeout;
        }

        internal void ResetMoveTimer()
        {
            if (HasMoveTimeLimit) MoveTimer = TotalTimer < MoveTimeLimit ? TotalTimer : MoveTimeLimit;
        }
    }
}
