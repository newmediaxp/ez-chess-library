namespace NMX.EzChess.Library.Core
{
    using System;

    public static class ChessNotation
    {

        public static char GetNotation(in ChessPieceType p_type, in bool p_caps = true)
        {
            char notation = p_type switch
            {
                ChessPieceType.Pawn => 'P',
                ChessPieceType.Rook => 'R',
                ChessPieceType.Knight => 'N',
                ChessPieceType.Bishop => 'B',
                ChessPieceType.Queen => 'Q',
                ChessPieceType.King => 'K',
                _ => throw new NotImplementedException(),
            };
            return p_caps ? notation : char.ToLower(notation);
        }

        public static ChessPieceType GetPieceType(in char p_notation) => char.ToUpper(p_notation) switch
        {
            'P' => ChessPieceType.Pawn,
            'R' => ChessPieceType.Rook,
            'N' => ChessPieceType.Knight,
            'B' => ChessPieceType.Bishop,
            'Q' => ChessPieceType.Queen,
            'K' => ChessPieceType.King,
            _ => throw new NotImplementedException(),
        };

        public static ChessPieceColor GetPieceColor(in char p_pieceNotation) => char.IsUpper(p_pieceNotation) ? ChessPieceColor.White : ChessPieceColor.Black;

        public static char GetNotation(in ChessPieceColor p_color) => p_color switch
        {
            ChessPieceColor.White => 'w',
            ChessPieceColor.Black => 'b',
            _ => throw new NotImplementedException(),
        };

        public static ChessPieceColor GetColorDirect(in char p_colorNotation) => char.ToLower(p_colorNotation) switch
        {
            'w' => ChessPieceColor.White,
            'b' => ChessPieceColor.Black,
            _ => throw new NotImplementedException(),
        };

        public static string GetNotation(in Coordinate2D p_position, in bool p_column = true, in bool p_row = true)
        {
            if (p_column && p_row) return $"{(char)(p_position.x + 96)}{p_position.y}";
            if (p_column) return $"{(char)(p_position.x + 96)}";
            if (p_row) return $"{p_position.y}";
            return string.Empty;
        }

        public static Coordinate2D? GetPosition(in string p_notation)
        {
            if (string.IsNullOrEmpty(p_notation) || p_notation.Length != 2) return null;
            int x = p_notation[0] - 96;
            if (!int.TryParse(p_notation.AsSpan(1, 1), out int y)) return null;
            Coordinate2D position = new Coordinate2D(x, y);
            if (!position.IsValid()) return null;
            return position;
        }

    }

}
