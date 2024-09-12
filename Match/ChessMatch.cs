namespace NMX.EzChess.Library.Match
{
    using Core;
    using NMX.EzChess.Library.Bot;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ChessMatch
    {
        private ChessMatchState boardState;
        private readonly Stack<ChessMatchState> playedBoardStates, undoBoardStates;
        private readonly CancellationTokenSource bgTasksCts;
        private const int clockRate = 10, minBotDelay = 100;
        public event Action? OnBoardUpdated, OnBotMoved, OnTimerUpdated;
        public bool whiteIsBot, blackIsBot;
        private const string
            msg_boardNotConfigured = "board not configured";
        public ChessBoard Board => boardState.board;
        public PlayTimer WhiteTimer => boardState.whiteTimer;
        public PlayTimer BlackTimer => boardState.blackTimer;
        private PlayTimer CurrentPlayTimer => Board.CurrentColor switch
        {
            ChessPieceColor.White => WhiteTimer,
            ChessPieceColor.Black => BlackTimer,
            _ => throw new NotImplementedException(),
        };
        public bool IsBotMove => Board.CurrentColor switch
        {
            ChessPieceColor.White => whiteIsBot,
            ChessPieceColor.Black => blackIsBot,
            _ => throw new NotImplementedException(),
        };
        public bool CanUndo => playedBoardStates.Count > 0;
        public bool CanRedo => undoBoardStates.Count > 0;


        public ChessMatch()
        {
            boardState = new ChessMatchState(new ChessBoard(), new PlayTimer(), new PlayTimer());
            playedBoardStates = new Stack<ChessMatchState>();
            undoBoardStates = new Stack<ChessMatchState>();
            bgTasksCts = new CancellationTokenSource();
            CheckPlayTimers(bgTasksCts.Token);
            HandleBotMoves(bgTasksCts.Token);
        }

        ~ChessMatch()
        {
            bgTasksCts.Cancel();
        }

        public void Configure(in string? p_fen, in int p_totalTimeLimit, in int p_moveTimeLimit)
        {
            playedBoardStates.Clear();
            undoBoardStates.Clear();
            Board.Configure(p_fen);
            OnBoardUpdated?.Invoke();
            WhiteTimer.Set(p_totalTimeLimit, p_moveTimeLimit);
            BlackTimer.Set(p_totalTimeLimit, p_moveTimeLimit);
            OnTimerUpdated?.Invoke();
        }

        public List<ChessMove>? GetMoves(in Coordinate2D p_position)
        {
            if (Board.MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (!p_position.IsValid()) return null;
            ChessPiece? _piece = Board.GetPieceAt(p_position.GetIndex());
            if (_piece?.color != Board.CurrentColor) return null;
            List<ChessMove> _moves = new List<ChessMove>(_piece!.type.GetMaxMoves());
            foreach (ChessMove _move in Board.GetMovesRef(Board.CurrentColor))
                if (_move.self_from.EquivalentTo(p_position)) _moves.Add(_move);
            return _moves;
        }

        public ChessMove? GetMoveFromNotation(in string p_moveNotation)
        {
            if (Board.MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (string.IsNullOrEmpty(p_moveNotation) || p_moveNotation.Length < 2) return null;
            List<ChessMove> _moves = Board.GetMovesRef(Board.CurrentColor);
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

        public ChessMove? GetRandomMove() => ChessBot.GetRandomMove(Board, Board.CurrentColor);

        public ChessMove? GetBestMove(in int p_depth) => ChessBot.GetBestMove(Board, Board.CurrentColor, p_depth);

        public string AnalyzeBestMove(in int p_depth) => ChessBot.AnalyzeBestMove(Board, Board.CurrentColor, p_depth);

        public bool IsPlayerInCheck() => Board.IsColorInCheck(Board.CurrentColor);

        public (bool p_error, string p_message) ApplyMove(in ChessMove p_move) => Board.ApplyMove(p_move);

        private async void HandleBotMoves(CancellationToken p_token)
        {
            while (!p_token.IsCancellationRequested)
            {
                if (Board.MatchStatus == ChessMatchStatus.Paused && IsBotMove) StartTurn();
                await Task.Yield();
                if (Board.MatchStatus == ChessMatchStatus.Running && IsBotMove)
                {
                    ChessMove? _move = GetRandomMove();
                    //ChessMove? _move = GetBestMove(4);
                    if (!_move.HasValue) throw new InvalidOperationException("bot has no move");
                    await Task.Delay(minBotDelay, p_token);
                    if (Board.MatchStatus != ChessMatchStatus.Running) continue;
                    (bool error, string message) = ApplyMove(_move.Value);
                    if (error) throw new InvalidOperationException($"error while applying bot move : {message}");
                    await Task.Yield();
                    if (Board.MatchStatus != ChessMatchStatus.Running) continue;
                    FinishTurn();
                    OnBotMoved?.Invoke(); 
                }
                await Task.Yield();
            }
        }

        private async void CheckPlayTimers(CancellationToken p_token)
        {
            while (!p_token.IsCancellationRequested)
            {
                await Task.Delay(clockRate, p_token);
                if (p_token.IsCancellationRequested) return;
                if (Board.MatchStatus == ChessMatchStatus.Running)
                { 
                    if (CurrentPlayTimer.DecrementTimers(clockRate)) TimeoutTurn();
                    OnTimerUpdated?.Invoke();
                }
            }
        }

        public void StartTurn()
        {
            CurrentPlayTimer.ResetMoveTimer();
            playedBoardStates.Push(boardState.CreateDeepClone());
            Board.StartTurn();
            OnBoardUpdated?.Invoke();
        }

        public void FinishTurn()
        {
            Board.FinishTurn();
            OnBoardUpdated?.Invoke();
        }

        public void TimeoutTurn()
        {
            Board.TimeoutTurn();
            OnBoardUpdated?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            undoBoardStates.Push(boardState.CreateDeepClone());
            boardState = playedBoardStates.Pop();
            OnBoardUpdated?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo) return;
            playedBoardStates.Push(boardState.CreateDeepClone());
            boardState = undoBoardStates.Pop();
            OnBoardUpdated?.Invoke();
        }

    }

}
