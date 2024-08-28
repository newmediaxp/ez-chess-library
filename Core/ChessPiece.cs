namespace NMX.EzChess.Library.Core
{
    using System.Collections.Generic;

    public sealed class ChessPiece
    {
        public readonly ChessPieceType type;
        public readonly ChessPieceColor color;
        private readonly int m_direction, m_limit, m_maxMoves;
        private bool m_lastMovedPiece;
        public Coordinate2D Position { get; private set; }
        public int MoveCount { get; private set; }

        private ChessPiece() { }

        public ChessPiece(in ChessPieceType p_type, in ChessPieceColor p_color, in Coordinate2D p_position)
        {
            type = p_type;
            color = p_color;
            Position = p_position;
            MoveCount = 0;
            m_lastMovedPiece = false;
            m_direction = p_color == ChessPieceColor.White ? 1 : -1;
            m_limit = System.Math.Max(ChessBoard.ROWS, ChessBoard.COLUMNS);
            m_maxMoves = p_type.GetMaxMoves();
        }

        internal ChessPiece CreateDeepClone() => new ChessPiece(type, color, Position)
        {
            MoveCount = MoveCount,
            m_lastMovedPiece = m_lastMovedPiece,
        };

        internal void ChangePosition(in Coordinate2D p_position)
        {
            Position = p_position;
            ++MoveCount;
        }

        internal void MarkAsLastMovedPiece(in ChessBoard p_board)
        {
            ChessPiece? piece;
            for (int i = 0; i < p_board.MaxPiecesCount; ++i)
            {
                piece = p_board.GetPieceAt(i);
                if (piece != null) piece.m_lastMovedPiece = piece == this;
            }
        }

        public List<ChessMove> FindMoves(in ChessBoard p_board) => type switch
        {
            ChessPieceType.Pawn => FindMoves_Pawn(p_board),
            ChessPieceType.Rook => FindMoves_Rook(p_board),
            ChessPieceType.Knight => FindMoves_Knight(p_board),
            ChessPieceType.Bishop => FindMoves_Bishop(p_board),
            ChessPieceType.Queen => FindMoves_Queen(p_board),
            ChessPieceType.King => FindMoves_King(p_board),
            _ => throw new System.NotImplementedException(),
        };

        private List<ChessMove> FindMoves_Pawn(in ChessBoard p_board)
        {
            List<ChessMove> moves = new List<ChessMove>(m_maxMoves);
            Coordinate2D testPos, enPassPos;
            ChessPiece? testPos_piece, enPassPos_piece;
            // forward tiles
            for (int t = 1; t <= 2; ++t)
            {
                testPos = new Coordinate2D(Position.x, Position.y + m_direction * t);
                if (!testPos.IsValid()) break;
                testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                if (testPos_piece != null || (t > 1 && MoveCount > 0)) break;
                if ((color == ChessPieceColor.White && testPos.y == ChessBoard.ROWS) || (color == ChessPieceColor.Black && testPos.y == 1))
                    moves.Add(new ChessMove(Position, testPos, ChessPieceType.Queen)); //PawnPromotion
                else moves.Add(new ChessMove(Position, testPos));
            }
            // corner tiles
            for (int d = 1; d <= 2; ++d)
            {
                testPos = d == 1 ? new Coordinate2D(Position.x + 1, Position.y + m_direction) : new Coordinate2D(Position.x - 1, Position.y + m_direction);
                enPassPos = d == 1 ? new Coordinate2D(Position.x + 1, Position.y) : new Coordinate2D(Position.x - 1, Position.y);
                if (!testPos.IsValid()) continue;
                testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                if (testPos_piece != null && testPos_piece.color != color) moves.Add(new ChessMove(Position, testPos));
                if (!enPassPos.IsValid()) continue;
                enPassPos_piece = p_board.GetPieceAt(enPassPos.GetIndex());
                if (testPos_piece == null && enPassPos_piece != null && enPassPos_piece.color != color && enPassPos_piece.type == ChessPieceType.Pawn && enPassPos_piece.m_lastMovedPiece && enPassPos_piece.MoveCount == 1) moves.Add(new ChessMove(Position, testPos, enPassPos)); //EnPassant
            }
            return moves;
        }

        private List<ChessMove> FindMoves_Rook(in ChessBoard p_board)
        {
            List<ChessMove> moves = new List<ChessMove>(m_maxMoves);
            Coordinate2D testPos;
            ChessPiece? testPos_piece;
            // horizontal and vertical tiles
            for (int d = 1; d <= 4; ++d)
            {
                for (int t = 1; t <= m_limit; ++t)
                {
                    testPos = d switch
                    {
                        1 => new Coordinate2D(Position.x + t, Position.y),
                        2 => new Coordinate2D(Position.x - t, Position.y),
                        3 => new Coordinate2D(Position.x, Position.y + t),
                        4 => new Coordinate2D(Position.x, Position.y - t),
                        _ => throw new System.NotImplementedException(),
                    };
                    if (!testPos.IsValid()) break;
                    testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                    if (testPos_piece != null)
                    {
                        if (testPos_piece.color != color) moves.Add(new ChessMove(Position, testPos));
                        break;
                    }
                    moves.Add(new ChessMove(Position, testPos));
                }
            }
            return moves;
        }

        private List<ChessMove> FindMoves_Knight(in ChessBoard p_board)
        {
            List<ChessMove> moves = new List<ChessMove>(m_maxMoves);
            Coordinate2D testPos;
            ChessPiece? testPos_piece;
            // surrounding tiles
            for (int d = 1; d <= 8; ++d)
            {
                testPos = d switch
                {
                    1 => new Coordinate2D(Position.x + 1, Position.y + 2),
                    2 => new Coordinate2D(Position.x - 1, Position.y + 2),
                    3 => new Coordinate2D(Position.x + 1, Position.y - 2),
                    4 => new Coordinate2D(Position.x - 1, Position.y - 2),
                    5 => new Coordinate2D(Position.x + 2, Position.y + 1),
                    6 => new Coordinate2D(Position.x + 2, Position.y - 1),
                    7 => new Coordinate2D(Position.x - 2, Position.y + 1),
                    8 => new Coordinate2D(Position.x - 2, Position.y - 1),
                    _ => throw new System.NotImplementedException(),
                };
                if (!testPos.IsValid()) continue;
                testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                if (testPos_piece == null || (testPos_piece != null && testPos_piece.color != color)) moves.Add(new ChessMove(Position, testPos));
            }
            return moves;
        }

        private List<ChessMove> FindMoves_Bishop(in ChessBoard p_board)
        {
            List<ChessMove> moves = new List<ChessMove>(m_maxMoves);
            Coordinate2D testPos;
            ChessPiece? testPos_piece;
            // diagonal tiles
            for (int d = 1; d <= 4; ++d)
            {
                for (int t = 1; t <= m_limit; ++t)
                {
                    testPos = d switch
                    {
                        1 => new Coordinate2D(Position.x + t, Position.y + t),
                        2 => new Coordinate2D(Position.x - t, Position.y + t),
                        3 => new Coordinate2D(Position.x + t, Position.y - t),
                        4 => new Coordinate2D(Position.x - t, Position.y - t),
                        _ => throw new System.NotImplementedException(),
                    };
                    if (!testPos.IsValid()) break;
                    testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                    if (testPos_piece != null)
                    {
                        if (testPos_piece.color != color) moves.Add(new ChessMove(Position, testPos));
                        break;
                    }
                    moves.Add(new ChessMove(Position, testPos));
                }
            }
            return moves;
        }

        private List<ChessMove> FindMoves_Queen(in ChessBoard p_board)
        {
            List<ChessMove> moves = new List<ChessMove>(m_maxMoves);
            Coordinate2D testPos;
            ChessPiece? testPos_piece;
            // horizontal, vertical and diagonal tiles
            for (int d = 1; d <= 8; ++d)
            {
                for (int t = 1; t <= m_limit; ++t)
                {
                    testPos = d switch
                    {
                        1 => new Coordinate2D(Position.x + t, Position.y),
                        2 => new Coordinate2D(Position.x - t, Position.y),
                        3 => new Coordinate2D(Position.x, Position.y + t),
                        4 => new Coordinate2D(Position.x, Position.y - t),
                        5 => new Coordinate2D(Position.x + t, Position.y + t),
                        6 => new Coordinate2D(Position.x - t, Position.y + t),
                        7 => new Coordinate2D(Position.x + t, Position.y - t),
                        8 => new Coordinate2D(Position.x - t, Position.y - t),
                        _ => throw new System.NotImplementedException(),
                    };
                    if (!testPos.IsValid()) break;
                    testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                    if (testPos_piece != null)
                    {
                        if (testPos_piece.color != color) moves.Add(new ChessMove(Position, testPos));
                        break;
                    }
                    moves.Add(new ChessMove(Position, testPos));
                }
            }
            return moves;
        }

        private List<ChessMove> FindMoves_King(in ChessBoard p_board)
        {
            List<ChessMove> moves = new List<ChessMove>(m_maxMoves);
            Coordinate2D testPos, cassMidPos, cassRookPos;
            ChessPiece? testPos_piece, cassMidPos_piece, cassRookPos_piece;
            // surrounding tiles
            for (int d = 1; d <= 8; ++d)
            {
                testPos = d switch
                {
                    1 => new Coordinate2D(Position.x + 1, Position.y),
                    2 => new Coordinate2D(Position.x - 1, Position.y),
                    3 => new Coordinate2D(Position.x, Position.y + 1),
                    4 => new Coordinate2D(Position.x, Position.y - 1),
                    5 => new Coordinate2D(Position.x + 1, Position.y + 1),
                    6 => new Coordinate2D(Position.x - 1, Position.y + 1),
                    7 => new Coordinate2D(Position.x + 1, Position.y - 1),
                    8 => new Coordinate2D(Position.x - 1, Position.y - 1),
                    _ => throw new System.NotImplementedException(),
                };
                if (!testPos.IsValid()) continue;
                testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                if (testPos_piece == null || (testPos_piece != null && testPos_piece.color != color)) moves.Add(new ChessMove(Position, testPos));
            }
            // castelling [{ need to include for logic }]
            for (int d = 1; d <= 2; ++d)
            {
                if (MoveCount > 0) break;
                testPos = d switch
                {
                    1 => new Coordinate2D(Position.x + 2, Position.y),
                    2 => new Coordinate2D(Position.x - 2, Position.y),
                    _ => throw new System.NotImplementedException(),
                };
                cassMidPos = d switch
                {
                    1 => new Coordinate2D(Position.x + 1, Position.y),
                    2 => new Coordinate2D(Position.x - 1, Position.y),
                    _ => throw new System.NotImplementedException(),
                };
                cassRookPos = d switch
                {
                    1 => new Coordinate2D(ChessBoard.COLUMNS, Position.y),
                    2 => new Coordinate2D(1, Position.y),
                    _ => throw new System.NotImplementedException(),
                };
                if (!testPos.IsValid() || !cassMidPos.IsValid() || !cassRookPos.IsValid()) continue;
                testPos_piece = p_board.GetPieceAt(testPos.GetIndex());
                cassMidPos_piece = p_board.GetPieceAt(cassMidPos.GetIndex());
                cassRookPos_piece = p_board.GetPieceAt(cassRookPos.GetIndex());
                if (testPos_piece != null || cassMidPos_piece != null || p_board.IsPositionInCheck(testPos, color) || p_board.IsPositionInCheck(cassMidPos, color)) continue;
                if (cassRookPos_piece == null || cassRookPos_piece.color != color || cassRookPos_piece.type != ChessPieceType.Rook || cassRookPos_piece.MoveCount > 0) continue;
                moves.Add(new ChessMove(Position, testPos, cassRookPos, cassMidPos));
            }
            return moves;
        }

    }

}
