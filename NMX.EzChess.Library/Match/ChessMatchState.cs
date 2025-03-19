namespace NMX.EzChess.Library.Match
{
    using NMX.EzChess.Library.Core;

    internal sealed class ChessMatchState
    {
        public readonly ChessBoard board;
        public readonly PlayTimer whiteTimer, blackTimer;

        public ChessMatchState(in ChessBoard p_board, in PlayTimer p_whiteTimer, in PlayTimer p_blackTimer)
        {
            board = p_board;
            whiteTimer = p_whiteTimer;
            blackTimer = p_blackTimer;
        }

        public ChessMatchState CreateDeepClone() => new ChessMatchState(
            board.CreateDeepClone(),
            whiteTimer.CreateDeepClone(),
            blackTimer.CreateDeepClone());
    }
}
