namespace NMX.EzChess.Library.IO
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class EZChessManagerCLI
    {
        private static ChessBoard s_board;
        private static bool
            s_rotation,
            s_started,
            s_terminate,
            s_whiteIsBot,
            s_blackIsBot;
        private static readonly Stopwatch s_stopwatch1;
        private const string
            k_msg_boardNotConfigured = "No Running Game",
            k_msg_boardNotPlayable = "No Playable Game",
            k_msg_positionInvalid = "position does not exist",
            k_msg_moveInvalid = "given move is invalid",
            k_msg_moveNotApplicable = "cannot apply given move";
        private const string
            k_appName = "Ez Chess",
            k_appVersion = "v0.2";

        private static bool NeedsRotation => s_rotation && s_board.CurrentColor == ChessPieceColor.Black;
        private static bool IsBotMove => s_board.CurrentColor switch
        {
            ChessPieceColor.White => s_whiteIsBot,
            ChessPieceColor.Black => s_blackIsBot,
            _ => throw new NotImplementedException(),
        };

        static EZChessManagerCLI()
        {
            s_board = new ChessBoard();
            s_rotation = false;
            s_whiteIsBot = false;
            s_blackIsBot = false;
            s_stopwatch1 = new Stopwatch();
        }

        public static void Start()
        {
            OutputHandlerCLI.Write($"[ Welcome to {k_appName} {k_appVersion} ]\n");
            s_started = true;
        }

        public static void H_Set(in bool? p_rotation = null, in bool? p_whiteIsBot = null, in bool? p_blackIsBot = null)
        {
            bool showBoard = false;
            if (p_rotation.HasValue)
            {
                OutputHandlerCLI.Write($"[ rotation : {s_rotation} -> {p_rotation} ]\n");
                s_rotation = p_rotation.Value;
                if (s_board.CurrentColor != ChessPieceColor.White) showBoard = true;
            }
            if (p_whiteIsBot.HasValue)
            {
                OutputHandlerCLI.Write($"[ whiteIsBot : {s_whiteIsBot} -> {p_whiteIsBot} ]\n");
                s_whiteIsBot = p_whiteIsBot.Value;
                if (s_board.CurrentColor == ChessPieceColor.White) showBoard = true;
            }
            if (p_blackIsBot.HasValue)
            {
                OutputHandlerCLI.Write($"[ blackIsBot : {p_blackIsBot} ]\n");
                s_blackIsBot = p_blackIsBot.Value;
                if (s_board.CurrentColor == ChessPieceColor.Black) showBoard = true;
            }
            if (showBoard && s_board.Configured) H_ShowBoard();
        }

        public static void H_Quit()
        {
            OutputHandlerCLI.Write($"[ {k_appName} Terminated ]");
            s_terminate = true;
        }

        public static void H_InputRecievedAck() => OutputHandlerCLI.Write("...\n");

        public static void H_InputInvalid() => OutputHandlerCLI.Write("[ Invalid Command ]\n");

        public static void H_StartGame(in string? p_fen = null)
        {
            s_board.ConfigureBoard(p_fen);
            OutputHandlerCLI.Write("[ Game Started ]");
            s_board.GenerateMoveNotation = true;
            s_board.StartTurn();
            H_ShowBoard();
        }

        public static void H_ShowBoard(in Coordinate2D? p_mark_from = null, in Coordinate2D[]? p_marks_to = null)
        {
            if (!s_board.Configured) { OutputHandlerCLI.Write($"[ {k_msg_boardNotConfigured} ]\n"); return; }
            ChessMove? botMove = null;
            if (IsBotMove)
            {
                if (!s_board.Playable || s_board.EndStatus != ChessMatchEndStatus.None) return;
                //move_bot = ChessBot.GetRandomMove(board, board.Current_Color);
                botMove = ChessBot.GetBestMove(s_board, s_board.CurrentColor, 4);
            }
            if (botMove.HasValue)
            {
                (bool error, string message) = s_board.ApplyMove(botMove.Value);
                if (error) { OutputHandlerCLI.Write($"( error: {message} )\n"); return; }
                s_board.FinishTurn();
                OutputHandlerCLI.Write($"( applied bot move : {s_board.LastMoveNotation} )  |  turns = {s_board.TurnCount}");
            }
            OutputHandlerCLI.Write(OutputHandlerCLI.GetVisual(s_board, NeedsRotation, p_mark_from, p_marks_to));
            if (botMove.HasValue)
            {
                s_board.StartTurn();
                H_ShowBoard();
            }
        }

        public static void H_ExportBoard()
        {
            if (!s_board.Configured) { OutputHandlerCLI.Write($"[ {k_msg_boardNotConfigured} ]\n"); return; }
            OutputHandlerCLI.Write(s_board.FEN);
            OutputHandlerCLI.Write(null);
        }

        public static void H_ImportBoard(in string p_fen)
        {
            ChessBoard? backupBoard = s_board.Configured ? s_board.CreateDeepClone() : new ChessBoard();
            OutputHandlerCLI.Write($"( importing... )\n");
            try
            {
                H_StartGame(p_fen);
            }
            catch (Exception e)
            {
                OutputHandlerCLI.Write($"( import failed ) : {e.Message}\n");
                s_board = backupBoard;
            }
        }

        public static void H_ShowMovesForCurrentPlayer(in Coordinate2D p_position)
        {
            if (!p_position.IsValid()) { OutputHandlerCLI.Write($"( {k_msg_positionInvalid} )\n"); return; }
            List<ChessMove>? moves = s_board.GetMovesAt(s_board.CurrentColor, p_position);
            if (moves == null) { OutputHandlerCLI.Write($"( { k_msg_positionInvalid} : no piece )\n"); return; }
            OutputHandlerCLI.Write("( marked moves for given position )");
            H_ShowBoard(p_position, moves.Select(move => move.self_to).ToArray());
        }

        public static void H_ApplyMoveForCurrentPlayer(in Coordinate2D p_from, in Coordinate2D p_to)
        {
            if (!s_board.Configured || !s_board.Playable) { OutputHandlerCLI.Write($"[ {k_msg_boardNotConfigured} ]\n"); return; }
            if (IsBotMove) { OutputHandlerCLI.Write($"( {k_msg_moveNotApplicable} : bot turn )\n"); return; }
            if (!p_from.IsValid() || !p_to.IsValid()) { OutputHandlerCLI.Write($"( {k_msg_moveNotApplicable} : positions invalid )\n"); return; }
            List<ChessMove>? moves = s_board.GetMovesAt(s_board.CurrentColor, p_from);
            if (moves == null) { OutputHandlerCLI.Write($"( {k_msg_moveNotApplicable} : no piece )\n"); return; }
            ChessMove? foundMove = null;
            foreach (ChessMove move in moves)
                if (move.self_from.EquivalentTo(p_from) && move.self_to.EquivalentTo(p_to)) { foundMove = move; break; }
            if (!foundMove.HasValue) { OutputHandlerCLI.Write($"( {k_msg_moveInvalid} )\n"); return; }
            (bool error, string message) = s_board.ApplyMove(foundMove.Value);
            if (error) { OutputHandlerCLI.Write($"( error while applying move : {message} )\n"); return; }
            s_board.FinishTurn();
            OutputHandlerCLI.Write($"( applied given move : {s_board.LastMoveNotation} )  |  turns = {s_board.TurnCount}");
            H_ShowBoard();
            s_board.StartTurn();
            OutputHandlerCLI.Write(s_board.EndStatus switch
            {
                ChessMatchEndStatus.None => string.Empty,
                ChessMatchEndStatus.WhiteCheckmated => "[ Match Ended =>  Black Won  :  White Checkmated ]",
                ChessMatchEndStatus.BlackCheckmated => "[ Match Ended =>  Black Checkmated  :  White Won ]",
                ChessMatchEndStatus.WhiteResigned => "[ Match Ended =>  White Resigned  :  Black Won ]",
                ChessMatchEndStatus.BlackResigned => "[ Match Ended =>  Black Resigned  :  White Won ]",
                ChessMatchEndStatus.WhiteTimedout => "[ Match Ended =>  White Timed Out  :  Black Won ]",
                ChessMatchEndStatus.BlackTimedout => "[ Match Ended =>  Black Timed Out  :  White Won ]",
                ChessMatchEndStatus.Stalemate => "[ Match Ended =>  Stalemate  :  Draw ]",
                ChessMatchEndStatus.Agreement => "[ Match Ended =>  Agreement  :  Draw ]",
                ChessMatchEndStatus.InsuffecientMaterial => "[ Match Ended =>  Insuffecient Material  :  Draw ]",
                ChessMatchEndStatus.NoCaptureInLast50Moves => "[ Match Ended =>  No Capture In Last 50 Moves  :  Draw ]",
                ChessMatchEndStatus.ThreefoldRepetition => "[ Match Ended =>  3 Fold Repetition  :  Draw ]",
                _ => throw new NotImplementedException(),
            });
        }

        public static void H_Test_SearchMove()
        {
            if (!s_board.Configured || !s_board.Playable) { OutputHandlerCLI.Write($"[ {k_msg_boardNotPlayable} ]\n"); return; }
            OutputHandlerCLI.Write("( SearchMove Test running ... )\n");
            for (int depth = 0; depth <= 4; ++depth)
            {
                s_stopwatch1.Restart();
                string searchInfo = ChessBot.SearchMove_Test(s_board, s_board.CurrentColor, depth);
                s_stopwatch1.Stop();
                OutputHandlerCLI.Write($"{searchInfo}\t| Time: {s_stopwatch1.ElapsedMilliseconds} ms");
            }
            OutputHandlerCLI.Write("\n( SearchMove Test successfull )\n");
        }

        public static void H_Test_MoveFromNotation(in string p_moveNotation)
        {
            if (!s_board.Configured || !s_board.Playable) { OutputHandlerCLI.Write($"[ {k_msg_boardNotPlayable} ]\n"); return; }
            ChessMove? move = s_board.GetChessMoveFromMoveNotation(p_moveNotation);
            if (!move.HasValue) { OutputHandlerCLI.Write("( invalid move notation )\n"); return; }
            OutputHandlerCLI.Write($"( move found : {ChessNotation.GetNotation(move.Value.self_from)}->{ChessNotation.GetNotation(move.Value.self_to)} )\n");
        }

        public static void Main()
        {
            Start();
            while (!s_terminate) InputHandlerCLI.ProcessInputs();
        }

    }
}
