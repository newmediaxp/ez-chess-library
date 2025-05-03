namespace NMX.EzChess.Console.Core;

using Library.Core;
using Library.Match;
using System;
using System.Text;

internal static class OutputHandler
{
    private static string message = string.Empty, board = string.Empty, timer = string.Empty;

    private static string GetVisual(in ChessPiece? p_piece)
    {
        if (p_piece == null) return "  ";
        StringBuilder _visual = new StringBuilder();
        _ = _visual.Append(p_piece.color switch
        {
            ChessPieceColor.White => ':',
            ChessPieceColor.Black => '.',
            _ => throw new NotImplementedException(),
        });
        _ = _visual.Append(ChessNotation.GetNotation(p_piece.type, p_piece.color == ChessPieceColor.White));
        return _visual.ToString();
    }

    private static string GetVisual(in ChessBoard p_board, in bool p_rotated = false, in Coordinate2D? p_mark_from = null, in Coordinate2D[]? p_marks_to = null)
    {
        if (p_board == null) return string.Empty;
        StringBuilder _boardVisual = new();
        StringBuilder[] _rowVisual = new StringBuilder[ChessBoard.rows]; // seperate visual for each row
        for (int i_row = 0; i_row < _rowVisual.Length; ++i_row) _rowVisual[i_row] = new();
        int i_rank = -1;
        for (int i_p = 0; i_p < ChessBoard.maxPiecesCount; ++i_p)
        {
            int i_piece = p_rotated ? ChessBoard.maxPiecesCount - 1 - i_p : i_p;
            Coordinate2D _piecePos = i_piece.GetPosition();
            ChessPiece? _piece = p_board.GetPieceAt(i_piece);
            // adding two extra lines for each row of pieces. one for the border, another for the markings
            if (i_rank != _piecePos.y - 1)
            {
                i_rank = _piecePos.y - 1;
                _ = _rowVisual[i_rank].AppendLine().Append("    +");
                // adding top border for row visuals
                for (int i_col = 0; i_col < ChessBoard.columns; ++i_col) _ = _rowVisual[i_rank].Append("-----+");
                // adding row rank brfore starting with the row and also extra space line
                _ = _rowVisual[i_rank].AppendLine().Append($"  {_piecePos.y} |");
                for (int i_col = 0; i_col < ChessBoard.columns; ++i_col)
                {
                    Coordinate2D _colPos = new(p_rotated ? ChessBoard.columns - i_col : i_col + 1, _piecePos.y);
                    ChessPiece? _colPiece = p_board.GetPieceAt(_colPos.GetIndex());
                    // marking if king if checked
                    if ((p_board.WhiteCheckedPosition.HasValue && _colPos.EquivalentTo(p_board.WhiteCheckedPosition.Value))
                        || (p_board.BlackCheckedPosition.HasValue && _colPos.EquivalentTo(p_board.BlackCheckedPosition.Value)))
                        _ = _rowVisual[i_rank].Append("   # |");
                    // marking selected piece
                    else if (p_mark_from.HasValue && p_mark_from.Value.EquivalentTo(_colPos)) _ = _rowVisual[i_rank].Append("   o |");
                    // marking possible moves for selected piece
                    else if (p_marks_to != null && p_marks_to.ContainsEquivalent(_colPos)) _ = _rowVisual[i_rank].Append("   * |");
                    // marking if piece was last moved
                    //else if (_piece != null) _ = _rowVisual[i_rank].Append($"  {_colPos.x}{_colPos.y} |");
                    else if (_colPiece != null && _colPiece.LastMoved) _ = _rowVisual[i_rank].Append($"   ~ |");
                    // blank line if nothing else
                    else _ = _rowVisual[i_rank].Append("     |");
                }
                // adding border for the first tile in a row for symetry
                _ = _rowVisual[i_rank].AppendLine().Append("    |");
            }
            // adding piece code at correct index
            _ = _rowVisual[i_rank].Append($" {GetVisual(_piece)}  |");
        }
        // taking reverse order of rows visuals if not rotated or else mormal orders
        for (int i_row = 0; i_row < _rowVisual.Length; ++i_row)
            _ = _boardVisual.Append(_rowVisual[p_rotated ? i_row : _rowVisual.Length - 1 - i_row]);
        // adding an additional bottom border for symetry
        _ = _boardVisual.AppendLine().Append("    +");
        for (int i_col = 0; i_col < ChessBoard.columns; i_col++) _ = _boardVisual.Append("-----+");
        // adding column files at the bottom
        _ = _boardVisual.AppendLine().Append("  .  ");
        for (int i_col = 0; i_col < ChessBoard.columns; i_col++)
            _ = _boardVisual.Append($"  {(char)(96 + (p_rotated ? ChessBoard.columns - i_col : i_col + 1))}   ");
        _ = _boardVisual.AppendLine();
        // adding turn info and game situation if checkmate, stalemate or some player is in check
        StringBuilder _infoVisual = new($"  Turn(#{p_board.TurnCount + 1}) : {p_board.CurrentColor}");
        if (p_board.WhiteCheckedPosition.HasValue || p_board.BlackCheckedPosition.HasValue) _ = _infoVisual.Append("  |  In Check");
        // adding moves counts if marking moves
        if (p_marks_to != null) _ = _infoVisual.Append($"  |  Moves : {p_marks_to.Length}");
        _infoVisual.AppendLine().Append($"[ Match {p_board.MatchStatus switch
        {
            ChessMatchStatus.NotConfigured => "Not Configured",
            ChessMatchStatus.Paused => "Paused",
            ChessMatchStatus.Running => "Running",
            ChessMatchStatus.WhiteCheckmated => "Ended =>  White Checkmated  :  Black Won",
            ChessMatchStatus.BlackCheckmated => "Ended =>  Black Checkmated  :  White Won",
            ChessMatchStatus.WhiteResigned => "Ended =>  White Resigned  :  Black Won",
            ChessMatchStatus.BlackResigned => "Ended =>  Black Resigned  :  White Won",
            ChessMatchStatus.WhiteTimedout => "Ended =>  White Timed Out  :  Black Won",
            ChessMatchStatus.BlackTimedout => "Ended =>  Black Timed Out  :  White Won",
            ChessMatchStatus.Stalemate => "Ended =>  Stalemate  :  Draw",
            ChessMatchStatus.Agreement => "Ended =>  Agreement  :  Draw",
            ChessMatchStatus.InsuffecientMaterial => "Ended =>  Insuffecient Material  :  Draw",
            ChessMatchStatus.NoCaptureInLast50Moves => "Ended =>  No Capture In Last 50 Moves  :  Draw",
            ChessMatchStatus.ThreefoldRepetition => "Ended =>  3 Fold Repetition  :  Draw",
            _ => throw new NotImplementedException(),
        }} ]");
        return _boardVisual.AppendLine().Append(_infoVisual).AppendLine().ToString();
    }

    public static void UpdateTitle(in string title) => Console.Title = title;

    private static void RefreshScreen()
    {
        Console.Write("\f\u001bc\x1b[3J");
        //Console.Clear();
        Console.WriteLine(board);
        Console.WriteLine(timer);
        Console.WriteLine(message);
        Console.Write(">> ");
    }

    public static void ClearMessage()
    {
        message = string.Empty;
        RefreshScreen();
    }

    public static void AddMessage(in string p_message)
    {
        message += $"<< {p_message}\n";
        RefreshScreen();
    }

    public static void UpdateBoard(in ChessBoard p_board, in bool p_rotated = false, in Coordinate2D? p_mark_from = null, in Coordinate2D[]? p_marks_to = null)
    {
        board = GetVisual(p_board, p_rotated, p_mark_from, p_marks_to);
        RefreshScreen();
    }

    public static void UpdateTimer(in PlayTimer p_whiteTimer, in PlayTimer p_blackTimer)
    {
        StringBuilder _timerVisual = new();
        _timerVisual.AppendLine().Append($"White => T : ({p_whiteTimer.TotalTimer}/{p_whiteTimer.TotalTimeLimit})ms  | M : ({p_whiteTimer.MoveTimer}/{p_whiteTimer.MoveTimeLimit})ms");
        _timerVisual.AppendLine().Append($"Black => T : ({p_blackTimer.TotalTimer}/{p_blackTimer.TotalTimeLimit})ms  | M : ({p_blackTimer.MoveTimer}/{p_blackTimer.MoveTimeLimit})ms");
        timer = $"[ {_timerVisual} ]";
        RefreshScreen();
    }

}
