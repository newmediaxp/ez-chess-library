namespace NMX.EzChess.Library.Match
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ChessMatch
    {
        private ChessBoardState boardState;
        private readonly Stack<ChessBoardState> playedBoardStates, undoBoardStates;
        private readonly Stopwatch turnSwc;
        private readonly CancellationTokenSource playTimersCts;
        private const int clockRate = 10;
        private const string
            msg_boardNotConfigured = "board not configured",
            msg_boardNotPlayable = "board not playable";
        public ChessBoard Board => boardState.board;
        public PlayTimer WhiteTimer => boardState.whiteTimer;
        public PlayTimer BlackTimer => boardState.blackTimer;
        private PlayTimer CurrentPlayTimer => Board.CurrentColor switch
        {
            ChessPieceColor.White => WhiteTimer,
            ChessPieceColor.Black => BlackTimer,
            _ => throw new NotImplementedException(),
        };
        public bool CanUndo => playedBoardStates.Count > 0;
        public bool CanRedo => undoBoardStates.Count > 0;

        public ChessMatch()
        {
            boardState = new ChessBoardState(new ChessBoard(), new PlayTimer(), new PlayTimer());
            playedBoardStates = new Stack<ChessBoardState>();
            undoBoardStates = new Stack<ChessBoardState>();
            turnSwc = new Stopwatch();
            playTimersCts = new CancellationTokenSource();
            CheckPlayTimers(playTimersCts.Token);
        }

        ~ChessMatch()
        {
            turnSwc.Stop();
            playTimersCts.Cancel();
        }

        public void Configure(in string p_fen, in int p_totalTimeLimit, in int p_moveTimeLimit)
        {
            playedBoardStates.Clear();
            undoBoardStates.Clear();
            Board.Configure(p_fen);
            WhiteTimer.Set(p_totalTimeLimit, p_moveTimeLimit);
            BlackTimer.Set(p_totalTimeLimit, p_moveTimeLimit);
        }

        public List<ChessMove>? GetMoves(in ChessPieceColor p_color, in Coordinate2D p_position)
        {
            if (Board.MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (!p_position.IsValid()) return null;
            ChessPiece? _piece = Board.GetPieceAt(p_position.GetIndex());
            if (_piece?.color != p_color) return null;
            List<ChessMove> _moves = new List<ChessMove>(_piece!.type.GetMaxMoves());
            foreach (ChessMove _move in Board.GetMovesRef(p_color))
                if (_move.self_from.EquivalentTo(p_position)) _moves.Add(_move);
            return _moves;
        }

        public ChessMove? GetMoveFromNotation(in ChessPieceColor p_color, in string p_moveNotation)
        {
            if (Board.MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (string.IsNullOrEmpty(p_moveNotation) || p_moveNotation.Length < 2) return null;
            List<ChessMove> _moves = Board.GetMovesRef(p_color);
            try
            {
                if (p_moveNotation == ChessNotation.kingSideCastelling)
                    return _moves.Find(move => move.other_from.HasValue && move.other_to.HasValue && move.self_from.x < move.self_to.x);
                if (p_moveNotation == ChessNotation.queenSideCastelling)
                    return _moves.Find(move => move.other_from.HasValue && move.other_to.HasValue && move.self_from.x > move.self_to.x);
                ChessPieceType _type = ChessNotation.GetPieceType(p_moveNotation[0]);
                int i_from, i_to, i_row = -1, i_column = -1, i_x = p_moveNotation.IndexOf(ChessNotation.capture);
                for (i_to = p_moveNotation.Length - 1; i_to >= 0 && i_to > i_x; --i_to)
                {
                    if (p_moveNotation[i_to] == ChessNotation.spawn || p_moveNotation[i_to] == ChessNotation.check
                        || p_moveNotation[i_to] == ChessNotation.checkmate) continue;
                    if (i_row == -1 && char.IsDigit(p_moveNotation[i_to]))
                    { i_row = int.Parse(p_moveNotation.AsSpan(i_to, 1)); continue; }
                    if (i_column == -1 && char.IsLetter(p_moveNotation[i_to]) && char.IsLower(p_moveNotation[i_to]))
                    { i_column = p_moveNotation[i_to] - 96; continue; }
                    if (i_row != -1 && i_column != -1) break;
                }
                Coordinate2D _self_to = new Coordinate2D(i_column, i_row);
                if (!_self_to.IsValid()) return null;
                i_row = -1; i_column = -1;
                for (i_from = 0; i_from < p_moveNotation.Length && i_from <= i_to; ++i_from)
                {
                    if (i_from == 0 && _type != ChessPieceType.Pawn) continue;
                    if (i_x != -1 && i_from >= i_x) break;
                    if (i_row == -1 && char.IsDigit(p_moveNotation[i_from]))
                    { i_row = int.Parse(p_moveNotation.AsSpan(i_from, 1)); continue; }
                    if (i_column == -1 && char.IsLetter(p_moveNotation[i_from]) && char.IsLower(p_moveNotation[i_from]))
                    { i_column = p_moveNotation[i_from] - 96; continue; }
                }
                if (i_column == -1 && i_row == -1)
                    return _moves.Find(move => move.self_to.EquivalentTo(_self_to) && Board.GetPieceAt(move.self_from.GetIndex())!.type == _type);
                if (i_column == -1)
                    return _moves.Find(move => move.self_to.EquivalentTo(_self_to) && Board.GetPieceAt(move.self_from.GetIndex())!.type == _type
                    && move.self_from.y == i_row);
                if (i_row == -1)
                    return _moves.Find(move => move.self_to.EquivalentTo(_self_to) && Board.GetPieceAt(move.self_from.GetIndex())!.type == _type
                    && move.self_from.x == i_column);
                return _moves.Find(move => move.self_to.EquivalentTo(_self_to) && Board.GetPieceAt(move.self_from.GetIndex())!.type == _type
                    && move.self_from.x == i_column && move.self_from.y == i_row);
            }
            catch { return null; }
        }

        public (bool p_error, string p_message) ApplyMove(in ChessMove p_move) => Board.ApplyMove(p_move);

        private async void CheckPlayTimers(CancellationToken p_token = default)
        {
            while (!p_token.IsCancellationRequested)
            {
                await Task.Delay(clockRate, p_token);
                if (p_token.IsCancellationRequested) return;
                if (Board.MatchStatus == ChessMatchStatus.Running && CurrentPlayTimer.DecrementTimers(clockRate)) Board.TimeoutTurn();
            }
        }

        public void StartTurn()
        {
            CurrentPlayTimer.ResetMoveTimer();
            Board.StartTurn();
        }

        public void FinishTurn()
        {
            Board.FinishTurn();
            CurrentPlayTimer.ResetMoveTimer();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            undoBoardStates.Push(boardState);
            boardState = playedBoardStates.Pop();
        }

        public void Redo()
        {
            if (!CanRedo) return;
            playedBoardStates.Push(boardState);
            boardState = undoBoardStates.Pop();
        }

    }

}
