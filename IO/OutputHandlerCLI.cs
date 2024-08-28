namespace NMX.EzChess.Library.IO
{
    using Core;
    using System;
    using System.Text;

    public static class OutputHandlerCLI
    {

        public static void Write(in string? p_message)
        {
            //Console.WriteLine(p_message);
            System.Diagnostics.Debug.WriteLine(p_message);
        }

        private static string GetVisual(in ChessPiece? p_piece)
        {
            if (p_piece == null) return "  ";
            StringBuilder visual = new StringBuilder();
            _ = visual.Append(p_piece.color switch
            {
                ChessPieceColor.White => ':',
                ChessPieceColor.Black => '.',
                _ => throw new NotImplementedException(),
            });
            _ = visual.Append(ChessNotation.GetNotation(p_piece.type, p_piece.color == ChessPieceColor.White));
            return visual.ToString();
        }

        public static string GetVisual(in ChessBoard p_board, in bool p_rotated = false, in Coordinate2D? p_mark_from = null, in Coordinate2D[]? p_marks_to = null)
        {
            if (p_board == null) return string.Empty;
            StringBuilder totalVisual = new StringBuilder();
            StringBuilder[] rowVisual = new StringBuilder[ChessBoard.ROWS]; // seperate visual for each row
            for (int i = 0; i < rowVisual.Length; ++i) rowVisual[i] = new StringBuilder();
            int i_rank = -1;
            for (int i = 0; i < p_board.MaxPiecesCount; ++i)
            {
                int i_piece = p_rotated ? p_board.MaxPiecesCount - 1 - i : i;
                Coordinate2D piecePosition = i_piece.GetPosition();
                // adding two extra lines for each row of pieces. one for the border, another for the markings
                if (i_rank != piecePosition.y - 1)
                {
                    i_rank = piecePosition.y - 1;
                    _ = rowVisual[i_rank].Append("\n    +");
                    // adding top border for row visuals
                    for (int c = 0; c < ChessBoard.COLUMNS; ++c) _ = rowVisual[i_rank].Append("-----+");
                    // adding row rank brfore starting with the row and also extra space line
                    _ = rowVisual[i_rank].Append($"\n  {piecePosition.y} |");
                    for (int c = 0; c < ChessBoard.COLUMNS; ++c)
                    {
                        Coordinate2D cPos = new Coordinate2D(p_rotated ? ChessBoard.COLUMNS - c : c + 1, piecePosition.y);
                        // marking if king if checked
                        if ((p_board.WhiteCheckedPosition.HasValue && cPos.EquivalentTo(p_board.WhiteCheckedPosition.Value))
                            || (p_board.BlackCheckedPosition.HasValue && cPos.EquivalentTo(p_board.BlackCheckedPosition.Value)))
                            _ = rowVisual[i_rank].Append("   # |");
                        // marking selected piece
                        else if (p_mark_from.HasValue && p_mark_from.Value.EquivalentTo(cPos)) _ = rowVisual[i_rank].Append("   o |");
                        // marking possible moves for selected piece
                        else if (p_marks_to != null && p_marks_to.ContainsEquivalent(cPos)) _ = rowVisual[i_rank].Append("   * |");
                        // blank line if nothing else
                        else _ = rowVisual[i_rank].Append("     |");
                    }
                    // adding border for the first tile in a row for symetry
                    _ = rowVisual[i_rank].Append("\n    |");
                }
                // adding piece code at correct index
                _ = rowVisual[i_rank].Append($" {GetVisual(p_board.GetPieceAt(i_piece))}  |");
            }
            // taking reverse order of rows visuals if not rotated or else mormal orders
            for (int i = 0; i < rowVisual.Length; ++i) _ = totalVisual.Append(rowVisual[p_rotated ? i : rowVisual.Length - 1 - i]);
            // adding an additional bottom border for symetry
            _ = totalVisual.Append("\n    +");
            for (int c = 0; c < ChessBoard.COLUMNS; c++) _ = totalVisual.Append("-----+");
            // adding column files at the bottom
            _ = totalVisual.Append("\n  .  ");
            for (int c = 0; c < ChessBoard.COLUMNS; c++) _ = totalVisual.Append($"  {(char)(96 + (p_rotated ? ChessBoard.COLUMNS - c : c + 1))}   ");
            _ = totalVisual.Append("\n");
            // adding turn info and game situation if checkmate, stalemate or some player is in check
            StringBuilder infoVisual = new StringBuilder($"  Turn: {p_board.CurrentColor}");
            // adding moves counts if marking moves
            if (p_marks_to != null) _ = infoVisual.Append($"  |  Moves : {p_marks_to.Length}");
            if (p_board.WhiteCheckedPosition.HasValue || p_board.BlackCheckedPosition.HasValue) _ = infoVisual.Append("  |  In Check  |  Must Save King");
            _ = totalVisual.Append("\n" + infoVisual + "\n");
            //if (p_board.WhiteCheckedPosition.HasValue) Console.WriteLine($"--W:{(p_board.WhiteCheckedPosition.Value.x, p_board.WhiteCheckedPosition.Value.y)}\n");
            //if (p_board.BlackCheckedPosition.HasValue) Console.WriteLine($"--B:{(p_board.BlackCheckedPosition.Value.x, p_board.BlackCheckedPosition.Value.y)}\n");
            return totalVisual.ToString();
        }

    }
}
