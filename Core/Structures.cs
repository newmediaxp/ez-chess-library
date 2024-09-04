namespace NMX.EzChess.Library.Core
{
    using System;
    using System.Collections.Generic;


    public enum ChessPieceColor : byte
    {
        White,
        Black,
    }

    public enum ChessPieceType : byte
    {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King,
    }

    public enum ChessMatchStatus : byte
    {
        NotConfigured,
        Paused,
        Running,
        //Wins
        WhiteCheckmated,
        BlackCheckmated,
        WhiteResigned,
        BlackResigned,
        WhiteTimedout,
        BlackTimedout,
        //Draws
        Stalemate,
        Agreement,
        InsuffecientMaterial,
        NoCaptureInLast50Moves,
        ThreefoldRepetition,
    }

    public readonly struct Coordinate2D
    {
        public readonly bool initialized;
        public readonly int x, y;

        public Coordinate2D(in int p_x, in int p_y)
        {
            x = p_x;
            y = p_y;
            initialized = true;
        }

        public bool EquivalentTo(in Coordinate2D p_position) => x == p_position.x && y == p_position.y;
    }

    public enum ChessMoveType : byte
    {
        Normal,
        Enpassant,
        Castelling,
        PawnPromotion,
    }

    public readonly struct ChessMove
    {
        public readonly bool initialized;
        public readonly ChessMoveType type;
        public readonly Coordinate2D self_from, self_to;
        public readonly Coordinate2D? other_from, other_to;
        public readonly ChessPieceType? spawn;

        /// <summary>
        /// Avoid using this. Use the special ones instead.
        /// </summary>
        private ChessMove(in ChessMoveType p_type, in Coordinate2D p_self_from, in Coordinate2D p_self_to,
            in Coordinate2D? p_other_from, in Coordinate2D? p_other_to, in ChessPieceType? p_spawn)
        {
            type = p_type;
            self_from = p_self_from;
            self_to = p_self_to;
            other_from = p_other_from;
            other_to = p_other_to;
            spawn = p_spawn;
            initialized = true;
        }

        /// <returns>
        /// A ChessMove of Type: Normal
        /// </returns>
        public ChessMove(in Coordinate2D p_self_from, in Coordinate2D p_self_to)
            : this(ChessMoveType.Normal, p_self_from, p_self_to, null, null, null) { }

        /// <returns>
        /// A ChessMove of Type: Enpassant
        /// </returns>
        public ChessMove(in Coordinate2D p_self_from, in Coordinate2D p_self_to, in Coordinate2D? p_other_from)
            : this(ChessMoveType.Enpassant, p_self_from, p_self_to, p_other_from, null, null) { }

        /// <returns>
        /// A ChessMove of Type: Castelling
        /// </returns>
        public ChessMove(in Coordinate2D p_self_from, in Coordinate2D p_self_to, in Coordinate2D? p_other_from, in Coordinate2D? p_other_to)
            : this(ChessMoveType.Castelling, p_self_from, p_self_to, p_other_from, p_other_to, null) { }

        /// <returns>
        /// A ChessMove of Type: PawnPromotion
        /// </returns>
        public ChessMove(in Coordinate2D p_self_from, in Coordinate2D p_self_to, in ChessPieceType? p_spawn)
            : this(ChessMoveType.PawnPromotion, p_self_from, p_self_to, null, null, p_spawn) { }

        /// <summary>
        /// Check equivalance on the basis of <c>self_from</c>
        /// </summary>
        public bool EquivalentTo_SelfFrom(in ChessMove p_move) => self_from.EquivalentTo(p_move.self_from);

        /// <summary>
        /// Check equivalance on the basis of <c>self_to</c>
        /// </summary>
        public bool Equivalent_To(in ChessMove p_move) => self_to.EquivalentTo(p_move.self_to);
    }

    public static class CustomCollectionFunctions
    {
        /// <summary>
        /// Check if the list of positions contains a equivalent of the given position
        /// </summary>
        public static bool ContainsEquivalent(this IEnumerable<Coordinate2D> p_positions, in Coordinate2D p_position)
        {
            foreach (Coordinate2D position in p_positions) if (position.EquivalentTo(p_position)) return true;
            return false;
        }

        /// <summary>
        /// Check if the list of moves contains a move whose <c>self_from</c> is equivalant to the given position
        /// </summary>
        public static bool ContainsEquivalent_From(this IEnumerable<ChessMove> p_moves, in Coordinate2D p_position)
        {
            foreach (ChessMove move in p_moves) if (move.self_from.EquivalentTo(p_position)) return true;
            return false;
        }

        /// <summary>
        /// Check if the list of moves contains a move whose <c>self_to</c> is equivalant to the given position
        /// </summary>
        public static bool ContainsEquivalent_To(this IEnumerable<ChessMove> p_moves, in Coordinate2D p_position)
        {
            foreach (ChessMove move in p_moves) if (move.self_to.EquivalentTo(p_position)) return true;
            return false;
        }
    }

    public static class ChessBoardIndexer
    {
        private const int rows = ChessBoard.rows, columns = ChessBoard.columns;
        private const int
            maxMoves_Pawn = 2 + 2,                  // forward 2, captures 2
            maxMoves_Rook = rows + columns,         // 4 directions
            maxMoves_Knight = 4 * 2,                // 2 in each quadrant
            maxMoves_Bishop = rows + columns,       // 4 directions
            maxMoves_Queen = 2 * (rows + columns),  // 8 directions
            maxMoves_King = 8 + 2;                  // 8 directions, 2 castelling

        public static bool IsValid(this in Coordinate2D p_position) => p_position.initialized
            && p_position.x >= 1 && p_position.x <= rows && p_position.y >= 1 && p_position.y <= columns;

        public static int GetIndex(this in Coordinate2D p_position) => (p_position.y - 1) * columns + (p_position.x - 1);

        public static bool IsValid(this in int p_index) => p_index >= 0 && p_index < rows * columns;

        public static Coordinate2D GetPosition(this in int p_index) => new Coordinate2D((p_index % columns) + 1, (p_index / columns) + 1);

        public static ChessPieceColor Inverse(this ChessPieceColor p_color) => p_color switch
        {
            ChessPieceColor.White => ChessPieceColor.Black,
            ChessPieceColor.Black => ChessPieceColor.White,
            _ => throw new NotImplementedException(),
        };

        public static int GetMaxMoves(this ChessPieceType p_type) => p_type switch
        {
            ChessPieceType.Pawn => maxMoves_Pawn,
            ChessPieceType.Rook => maxMoves_Rook,
            ChessPieceType.Knight => maxMoves_Knight,
            ChessPieceType.Bishop => maxMoves_Bishop,
            ChessPieceType.Queen => maxMoves_Queen,
            ChessPieceType.King => maxMoves_King,
            _ => throw new NotImplementedException(),
        };

    }

}
