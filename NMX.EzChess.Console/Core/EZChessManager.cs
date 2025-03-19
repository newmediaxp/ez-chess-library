namespace NMX.EzChess.Console.Core;

using NMX.EzChess.Library.Core;
using NMX.EzChess.Library.Match;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

internal static class EZChessManager
{
    private static bool terminate;
    private static ChessMatch match;
    private static bool rotation;
    private static readonly Stopwatch stopwatch1;
    private const string
        msg_boardNotConfigured = "no running game",
        msg_boardNotPlayable = "no playable game",
        msg_positionInvalid = "position does not exist",
        msg_pieceInvalid = "no valid piece",
        msg_moveInvalid = "given move is invalid",
        msg_moveNotApplicable = "cannot apply given move";
    private const string
        appName = "Ez Chess",
        appVersion = "v0.4";

    private static bool NeedsRotation => rotation && match.Board.CurrentColor == ChessPieceColor.Black;

    static EZChessManager()
    {
        match = new();
        rotation = false;
        stopwatch1 = new Stopwatch();
        UpdateBoard();
        match.OnBoardUpdated += UpdateBoard;
        match.OnBotMoved += AppliedBotMove;
        //match.OnTimerUpdated += UpdateTimer;
    }

    public static void Main()
    {
        terminate = false;
        OutputHandler.UpdateTitle($"{appName} {appVersion}");
        OutputHandler.AddMessage($"Welcome to {appName} {appVersion}");
        while (!terminate) InputHandler.ProcessInput();
    }

    private static void UpdateBoard(in Coordinate2D? p_mark_from, in Coordinate2D[]? p_marks_to)
        => OutputHandler.UpdateBoard(match.Board, NeedsRotation, p_mark_from, p_marks_to);

    public static void UpdateBoard() => UpdateBoard(null, null);

    private static void AppliedBotMove()
    {
        OutputHandler.AddMessage($"{match.Board.CurrentColor.Inverse()} (bot) move : {match.Board.LastMoveNotation}");
        if (!match.IsBotMove) match.StartTurn();
    }

    private static void UpdateTimer() => OutputHandler.UpdateTimer(match.WhiteTimer, match.BlackTimer);

    public static void StartGame(in string? p_fen = null)
    {
        match.Configure(p_fen);
        OutputHandler.AddMessage("game started");
        match.Board.GenerateMoveNotation = true;
        if (!match.IsBotMove) match.StartTurn();
    }

    public static void Set(in bool? p_rotation = null, in bool? p_whiteIsBot = null, in bool? p_blackIsBot = null)
    {
        if (p_rotation.HasValue) OutputHandler.AddMessage($"{nameof(rotation)} : {rotation}->{rotation = p_rotation.Value}");
        if (p_whiteIsBot.HasValue) OutputHandler.AddMessage($"{nameof(match.config.whiteIsBot)} : {match.config.whiteIsBot}->{match.config.whiteIsBot = p_whiteIsBot.Value}");
        if (p_blackIsBot.HasValue) OutputHandler.AddMessage($"{nameof(match.config.blackIsBot)}  : {match.config.blackIsBot}->{match.config.blackIsBot = p_blackIsBot.Value}");
        UpdateBoard();
    }

    public static void Quit()
    {
        OutputHandler.AddMessage($"{appName} terminated");
        terminate = true;
    }

    public static void InputRecievedAck()
    {
        //OutputHandler.ClearMessage();
        //OutputHandler.AddMessage("...");
    }

    public static void InputInvalid() => OutputHandler.AddMessage("invalid command");


    public static void ExportBoard()
    {
        if (match.Board.MatchStatus == ChessMatchStatus.NotConfigured) { OutputHandler.AddMessage(msg_boardNotConfigured); return; }
        OutputHandler.AddMessage(match.Board.FEN);
    }

    public static void ImportBoard(in string p_fen)
    {
        ChessMatch _match = match;
        match = new ChessMatch();
        OutputHandler.AddMessage("importing...");
        try
        {
            StartGame(p_fen);
        }
        catch (Exception e)
        {
            OutputHandler.AddMessage($"import failed : {e.Message}");
            match = _match;
            UpdateBoard();
        }
    }

    public static void ShowMovesForCurrentPlayer(in Coordinate2D p_position)
    {
        if (!p_position.IsValid()) { OutputHandler.AddMessage(msg_positionInvalid); return; }
        List<ChessMove>? moves = match.GetMoves(p_position);
        if (moves == null) { OutputHandler.AddMessage(msg_pieceInvalid); return; }
        OutputHandler.AddMessage($"marked {moves.Count} moves for at {ChessNotation.GetNotation(p_position)}");
        UpdateBoard(p_position, moves.Select(move => move.self_to).ToArray());
    }

    public static void ApplyMoveForCurrentPlayer(in Coordinate2D p_from, in Coordinate2D p_to)
    {
        if (match.Board.MatchStatus != ChessMatchStatus.Running) { OutputHandler.AddMessage(msg_boardNotPlayable); return; }
        if (match.IsBotMove) { OutputHandler.AddMessage($"{msg_moveInvalid} : {match.Board.CurrentColor} (bot) turn"); return; }
        if (!p_from.IsValid() || !p_to.IsValid()) { OutputHandler.AddMessage($"{msg_moveInvalid} : {msg_positionInvalid}"); return; }
        List<ChessMove>? moves = match.GetMoves(p_from);
        if (moves == null) { OutputHandler.AddMessage($"{msg_moveInvalid} : {msg_pieceInvalid}"); return; }
        ChessMove? foundMove = null;
        foreach (ChessMove move in moves)
            if (move.self_from.EquivalentTo(p_from) && move.self_to.EquivalentTo(p_to)) { foundMove = move; break; }
        if (!foundMove.HasValue) { OutputHandler.AddMessage(msg_moveNotApplicable); return; }
        (bool error, string message) = match.ApplyMove(foundMove.Value);
        if (error) { OutputHandler.AddMessage($"error while applying move : {message}"); return; }
        match.FinishTurn();
        OutputHandler.AddMessage($"{match.Board.CurrentColor.Inverse()} move : {match.Board.LastMoveNotation}");
        if (!match.IsBotMove) match.StartTurn();
    }

    public static void SearchBestMoveTest()
    {
        if (match.Board.MatchStatus != ChessMatchStatus.Running) { OutputHandler.AddMessage($"[ {msg_boardNotPlayable} ]\n"); return; }
        OutputHandler.AddMessage("SearchMove Test running ...");
        for (int depth = 0; depth <= 5; ++depth)
        {
            stopwatch1.Restart();
            string searchInfo = match.AnalyzeBestMove(depth);
            stopwatch1.Stop();
            OutputHandler.AddMessage($"{searchInfo}\t| Time: {stopwatch1.ElapsedMilliseconds} ms");
        }
        OutputHandler.AddMessage("SearchMove Test successfull");
    }

    public static void GetMoveFromNotation(in string p_moveNotation)
    {
        if (match.Board.MatchStatus != ChessMatchStatus.Running) { OutputHandler.AddMessage(msg_boardNotPlayable); return; }
        ChessMove? move = match.GetMoveFromNotation(p_moveNotation);
        if (!move.HasValue) { OutputHandler.AddMessage(msg_moveInvalid); return; }
        OutputHandler.AddMessage($"move found : {ChessNotation.GetNotation(move.Value.self_from)}->{ChessNotation.GetNotation(move.Value.self_to)}");
    }

}
