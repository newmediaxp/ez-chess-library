namespace NMX.EzChess.Library.Core
{
    using System;
    using System.Collections.Generic;

    public sealed class ChessPiece
    {
        public readonly ChessPieceType type;
        public readonly ChessPieceColor color;
        public Coordinate2D Position { get; private set; }
        public bool LastMoved { get; private set; }
        private int moveCount;
        private readonly int direction, limit, maxMoves;


        private ChessPiece() { }

        internal ChessPiece(in ChessPieceType p_type, in ChessPieceColor p_color, in Coordinate2D p_position)
        {
            type = p_type;
            color = p_color;
            Position = p_position;
            LastMoved = false;
            moveCount = 0;
            direction = p_color == ChessPieceColor.White ? 1 : -1;
            limit = Math.Max(ChessBoard.rows, ChessBoard.columns);
            maxMoves = p_type.GetMaxMoves();
        }

        internal ChessPiece CreateDeepClone() => new ChessPiece(type, color, Position)
        {
            moveCount = moveCount,
            LastMoved = LastMoved,
        };

        internal void ChangePosition(in Coordinate2D p_position)
        {
            Position = p_position;
            ++moveCount;
        }

        internal void MarkAsLastMovedPiece(in ChessBoard p_board)
        {
            ChessPiece? _piece;
            for (int i = 0; i < ChessBoard.maxPiecesCount; ++i)
            {
                _piece = p_board.GetPieceAt(i);
                if (_piece != null) _piece.LastMoved = _piece == this;
            }
        }

        internal List<ChessMove> FindMoves(in ChessBoard p_board) => type switch
        {
            ChessPieceType.Pawn => FindMoves_Pawn(p_board),
            ChessPieceType.Rook => FindMoves_Rook(p_board),
            ChessPieceType.Knight => FindMoves_Knight(p_board),
            ChessPieceType.Bishop => FindMoves_Bishop(p_board),
            ChessPieceType.Queen => FindMoves_Queen(p_board),
            ChessPieceType.King => FindMoves_King(p_board),
            _ => throw new NotImplementedException(),
        };

        private List<ChessMove> FindMoves_Pawn(in ChessBoard p_board)
        {
            List<ChessMove> _moves = new List<ChessMove>(maxMoves);
            Coordinate2D _testPos, _enPassPos;
            ChessPiece? _testPos_piece, _enPassPos_piece;
            // forward tiles
            for (int i_f = 1; i_f <= 2; ++i_f)
            {
                _testPos = new Coordinate2D(Position.x, Position.y + direction * i_f);
                if (!_testPos.IsValid()) break;
                _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                if (_testPos_piece != null || (i_f > 1 && moveCount > 0)) break;
                if ((color == ChessPieceColor.White && _testPos.y == ChessBoard.rows) || (color == ChessPieceColor.Black && _testPos.y == 1))
                    _moves.Add(new ChessMove(Position, _testPos, ChessPieceType.Queen)); //PawnPromotion
                else _moves.Add(new ChessMove(Position, _testPos));
            }
            // corner tiles
            for (int i_c = 1; i_c <= 2; ++i_c)
            {
                _testPos = i_c == 1 ? new Coordinate2D(Position.x + 1, Position.y + direction) : new Coordinate2D(Position.x - 1, Position.y + direction);
                _enPassPos = i_c == 1 ? new Coordinate2D(Position.x + 1, Position.y) : new Coordinate2D(Position.x - 1, Position.y);
                if (!_testPos.IsValid()) continue;
                _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                if (_testPos_piece != null && _testPos_piece.color != color) _moves.Add(new ChessMove(Position, _testPos));
                if (!_enPassPos.IsValid()) continue;
                _enPassPos_piece = p_board.GetPieceAt(_enPassPos.GetIndex());
                if (_testPos_piece == null && _enPassPos_piece != null && _enPassPos_piece.color != color && _enPassPos_piece.type == ChessPieceType.Pawn && _enPassPos_piece.LastMoved && _enPassPos_piece.moveCount == 1) _moves.Add(new ChessMove(Position, _testPos, _enPassPos)); //EnPassant
            }
            return _moves;
        }

        private List<ChessMove> FindMoves_Rook(in ChessBoard p_board)
        {
            List<ChessMove> _moves = new List<ChessMove>(maxMoves);
            Coordinate2D _testPos;
            ChessPiece? _testPos_piece;
            // horizontal and vertical tiles
            for (int i_a = 1; i_a <= 4; ++i_a)
            {
                for (int i_f = 1; i_f <= limit; ++i_f)
                {
                    _testPos = i_a switch
                    {
                        1 => new Coordinate2D(Position.x + i_f, Position.y),
                        2 => new Coordinate2D(Position.x - i_f, Position.y),
                        3 => new Coordinate2D(Position.x, Position.y + i_f),
                        4 => new Coordinate2D(Position.x, Position.y - i_f),
                        _ => throw new NotImplementedException(),
                    };
                    if (!_testPos.IsValid()) break;
                    _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                    if (_testPos_piece != null)
                    {
                        if (_testPos_piece.color != color) _moves.Add(new ChessMove(Position, _testPos));
                        break;
                    }
                    _moves.Add(new ChessMove(Position, _testPos));
                }
            }
            return _moves;
        }

        private List<ChessMove> FindMoves_Knight(in ChessBoard p_board)
        {
            List<ChessMove> _moves = new List<ChessMove>(maxMoves);
            Coordinate2D _testPos;
            ChessPiece? _testPos_piece;
            // surrounding tiles
            for (int i_a = 1; i_a <= 8; ++i_a)
            {
                _testPos = i_a switch
                {
                    1 => new Coordinate2D(Position.x + 1, Position.y + 2),
                    2 => new Coordinate2D(Position.x - 1, Position.y + 2),
                    3 => new Coordinate2D(Position.x + 1, Position.y - 2),
                    4 => new Coordinate2D(Position.x - 1, Position.y - 2),
                    5 => new Coordinate2D(Position.x + 2, Position.y + 1),
                    6 => new Coordinate2D(Position.x + 2, Position.y - 1),
                    7 => new Coordinate2D(Position.x - 2, Position.y + 1),
                    8 => new Coordinate2D(Position.x - 2, Position.y - 1),
                    _ => throw new NotImplementedException(),
                };
                if (!_testPos.IsValid()) continue;
                _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                if (_testPos_piece == null || (_testPos_piece != null && _testPos_piece.color != color)) _moves.Add(new ChessMove(Position, _testPos));
            }
            return _moves;
        }

        private List<ChessMove> FindMoves_Bishop(in ChessBoard p_board)
        {
            List<ChessMove> _moves = new List<ChessMove>(maxMoves);
            Coordinate2D _testPos;
            ChessPiece? _testPos_piece;
            // diagonal tiles
            for (int i_a = 1; i_a <= 4; ++i_a)
            {
                for (int i_f = 1; i_f <= limit; ++i_f)
                {
                    _testPos = i_a switch
                    {
                        1 => new Coordinate2D(Position.x + i_f, Position.y + i_f),
                        2 => new Coordinate2D(Position.x - i_f, Position.y + i_f),
                        3 => new Coordinate2D(Position.x + i_f, Position.y - i_f),
                        4 => new Coordinate2D(Position.x - i_f, Position.y - i_f),
                        _ => throw new System.NotImplementedException(),
                    };
                    if (!_testPos.IsValid()) break;
                    _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                    if (_testPos_piece != null)
                    {
                        if (_testPos_piece.color != color) _moves.Add(new ChessMove(Position, _testPos));
                        break;
                    }
                    _moves.Add(new ChessMove(Position, _testPos));
                }
            }
            return _moves;
        }

        private List<ChessMove> FindMoves_Queen(in ChessBoard p_board)
        {
            List<ChessMove> _moves = new List<ChessMove>(maxMoves);
            Coordinate2D _testPos;
            ChessPiece? _testPos_piece;
            // horizontal, vertical and diagonal tiles
            for (int i_a = 1; i_a <= 8; ++i_a)
            {
                for (int i_f = 1; i_f <= limit; ++i_f)
                {
                    _testPos = i_a switch
                    {
                        1 => new Coordinate2D(Position.x + i_f, Position.y),
                        2 => new Coordinate2D(Position.x - i_f, Position.y),
                        3 => new Coordinate2D(Position.x, Position.y + i_f),
                        4 => new Coordinate2D(Position.x, Position.y - i_f),
                        5 => new Coordinate2D(Position.x + i_f, Position.y + i_f),
                        6 => new Coordinate2D(Position.x - i_f, Position.y + i_f),
                        7 => new Coordinate2D(Position.x + i_f, Position.y - i_f),
                        8 => new Coordinate2D(Position.x - i_f, Position.y - i_f),
                        _ => throw new NotImplementedException(),
                    };
                    if (!_testPos.IsValid()) break;
                    _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                    if (_testPos_piece != null)
                    {
                        if (_testPos_piece.color != color) _moves.Add(new ChessMove(Position, _testPos));
                        break;
                    }
                    _moves.Add(new ChessMove(Position, _testPos));
                }
            }
            return _moves;
        }

        private List<ChessMove> FindMoves_King(in ChessBoard p_board)
        {
            List<ChessMove> _moves = new List<ChessMove>(maxMoves);
            Coordinate2D _testPos, _cassMidPos, _cassRookPos;
            ChessPiece? _testPos_piece, _cassMidPos_piece, _cassRookPos_piece;
            // surrounding tiles
            for (int i_a = 1; i_a <= 8; ++i_a)
            {
                _testPos = i_a switch
                {
                    1 => new Coordinate2D(Position.x + 1, Position.y),
                    2 => new Coordinate2D(Position.x - 1, Position.y),
                    3 => new Coordinate2D(Position.x, Position.y + 1),
                    4 => new Coordinate2D(Position.x, Position.y - 1),
                    5 => new Coordinate2D(Position.x + 1, Position.y + 1),
                    6 => new Coordinate2D(Position.x - 1, Position.y + 1),
                    7 => new Coordinate2D(Position.x + 1, Position.y - 1),
                    8 => new Coordinate2D(Position.x - 1, Position.y - 1),
                    _ => throw new NotImplementedException(),
                };
                if (!_testPos.IsValid()) continue;
                _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                if (_testPos_piece == null || (_testPos_piece != null && _testPos_piece.color != color)) _moves.Add(new ChessMove(Position, _testPos));
            }
            // castelling tiles
            for (int i_c = 1; i_c <= 2; ++i_c)
            {
                if (moveCount > 0) break;
                _testPos = i_c switch
                {
                    1 => new Coordinate2D(Position.x + 2, Position.y),
                    2 => new Coordinate2D(Position.x - 2, Position.y),
                    _ => throw new System.NotImplementedException(),
                };
                _cassMidPos = i_c switch
                {
                    1 => new Coordinate2D(Position.x + 1, Position.y),
                    2 => new Coordinate2D(Position.x - 1, Position.y),
                    _ => throw new NotImplementedException(),
                };
                _cassRookPos = i_c switch
                {
                    1 => new Coordinate2D(ChessBoard.columns, Position.y),
                    2 => new Coordinate2D(1, Position.y),
                    _ => throw new NotImplementedException(),
                };
                if (!_testPos.IsValid() || !_cassMidPos.IsValid() || !_cassRookPos.IsValid()) continue;
                _testPos_piece = p_board.GetPieceAt(_testPos.GetIndex());
                _cassMidPos_piece = p_board.GetPieceAt(_cassMidPos.GetIndex());
                _cassRookPos_piece = p_board.GetPieceAt(_cassRookPos.GetIndex());
                if (_testPos_piece != null || _cassMidPos_piece != null || p_board.IsPositionInCheck(_testPos, color) || p_board.IsPositionInCheck(_cassMidPos, color)) continue;
                if (_cassRookPos_piece == null || _cassRookPos_piece.color != color || _cassRookPos_piece.type != ChessPieceType.Rook || _cassRookPos_piece.moveCount > 0) continue;
                _moves.Add(new ChessMove(Position, _testPos, _cassRookPos, _cassMidPos));
            }
            return _moves;
        }

    }

}
