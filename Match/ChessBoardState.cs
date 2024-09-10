namespace NMX.EzChess.Library.Match
{
    using NMX.EzChess.Library.Core;

    internal sealed class ChessBoardState
    {
        public readonly ChessBoard board;
        public readonly PlayTimer whiteTimer, blackTimer;

        public ChessBoardState(in ChessBoard p_board, in PlayTimer p_whiteTimer, in PlayTimer p_blackTimer)
        {
            board = p_board;
            whiteTimer = p_whiteTimer;
            blackTimer = p_blackTimer;
        }

        public ChessBoardState CreateDeepClone() => new ChessBoardState(
            board.CreateDeepClone(),
            whiteTimer.CreateDeepClone(),
            blackTimer.CreateDeepClone());
    }
}
