namespace NMX.EzChess.Library.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public sealed class ChessBoard
    {
        public const int rows = 8, columns = 8, maxPiecesCount = rows * columns;
        private readonly int maxMoves;
        private readonly ChessPiece?[] pieces;
        private readonly List<ChessMove> whiteMoves, blackMoves;
        private readonly StringBuilder moveNotation;
        private const string
            msg_boardNotConfigured = "board not configured",
            msg_boardNotPlayable = "board not playable",
            msg_positionInvalid = "position invalid",
            msg_turnInvalid = "turn is invalid",
            msg_moveInvalid = "move invalid",
            msg_moveNotApplicable = "move not applicable",
            msg_invalidFEN = "invalid FEN";
        public ChessMatchStatus MatchStatus { get; private set; }
        public ChessPieceColor CurrentColor { get; private set; }
        public Coordinate2D? WhiteCheckedPosition { get; private set; }
        public Coordinate2D? BlackCheckedPosition { get; private set; }
        public int TurnCount { get; private set; }
        public int PieceCount { get; private set; }
        public int LastMovedPieceIndex { get; private set; }
        public bool GenerateMoveNotation { get; set; }
        public string LastMoveNotation { get => moveNotation.ToString(); }
        public string FEN => GetFEN();


        internal ChessBoard()
        {
            pieces = new ChessPiece[maxPiecesCount];
            maxMoves = 2 * 8 * ChessPieceType.Pawn.GetMaxMoves()
                + 2 * 2 * ChessPieceType.Rook.GetMaxMoves()
                + 2 * 2 * ChessPieceType.Knight.GetMaxMoves()
                + 2 * 2 * ChessPieceType.Bishop.GetMaxMoves()
                + 2 * ChessPieceType.Queen.GetMaxMoves()
                + 2 * ChessPieceType.King.GetMaxMoves();
            whiteMoves = new List<ChessMove>(maxMoves);
            blackMoves = new List<ChessMove>(maxMoves);
            GenerateMoveNotation = false;
            moveNotation = new StringBuilder();
        }

        internal void Configure(in string? p_fen = null)
        {
            MatchStatus = ChessMatchStatus.NotConfigured;
            CurrentColor = ChessPieceColor.White;
            WhiteCheckedPosition = null;
            BlackCheckedPosition = null;
            TurnCount = 0;
            PieceCount = 0;
            LastMovedPieceIndex = -1;
            whiteMoves.Clear();
            blackMoves.Clear();
            SetFEN(!string.IsNullOrEmpty(p_fen) ? p_fen : ChessNotation.defaultFEN);
            CountPieces();
            IdentifyValidMoves(CurrentColor.Inverse());
            IdentifyValidMoves(CurrentColor);
            CheckIfGameOver();
            if (MatchStatus == ChessMatchStatus.NotConfigured) MatchStatus = ChessMatchStatus.Paused;
        }

        internal ChessBoard CreateDeepClone()
        {
            ChessBoard _board = new ChessBoard()
            {
                MatchStatus = MatchStatus,
                TurnCount = TurnCount,
                PieceCount = PieceCount,
                LastMovedPieceIndex = LastMovedPieceIndex,
                CurrentColor = CurrentColor,
                WhiteCheckedPosition = WhiteCheckedPosition,
                BlackCheckedPosition = BlackCheckedPosition,
            };
            for (int i_p = 0; i_p < pieces.Length; ++i_p) _board.pieces[i_p] = pieces[i_p]?.CreateDeepClone();
            return _board;
        }

        public ChessPiece? GetPieceAt(in int p_index) => p_index.IsValid() ? pieces[p_index] : null;

        public List<ChessMove> GetMovesRef(in ChessPieceColor p_color) => p_color switch
        {
            ChessPieceColor.White => whiteMoves,
            ChessPieceColor.Black => blackMoves,
            _ => throw new NotImplementedException(),
        };

        private string GetFEN()
        {
            if (MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            StringBuilder _fen = new StringBuilder();
            int _skipCount = 0;
            // get the pieces
            for (Coordinate2D _position = new Coordinate2D(1, rows); _position.IsValid();
                _position = _position.x < columns ? new Coordinate2D(_position.x + 1, _position.y) : new Coordinate2D(1, _position.y - 1))
            {
                int i_piece = _position.GetIndex();
                if (!i_piece.IsValid()) break;
                if (_position.y < rows && _position.x == 1)
                {
                    if (_skipCount > 0) _fen.Append(_skipCount);
                    _skipCount = 0;
                    _fen.Append('/');
                }
                if (pieces[i_piece] == null) { ++_skipCount; continue; }
                if (_skipCount > 0) _fen.Append(_skipCount);
                _skipCount = 0;
                char _pieceNotation = ChessNotation.GetNotation(pieces[i_piece]!.type);
                _fen.Append(pieces[i_piece]!.color == ChessPieceColor.White ? _pieceNotation : char.ToLower(_pieceNotation));
            }
            // get current color
            _fen.Append($" {ChessNotation.GetNotation(CurrentColor)} ");
            return _fen.ToString();
        }

        private void SetFEN(in string p_fen)
        {
            for (int i_p = 0; i_p < pieces.Length; ++i_p) pieces[i_p] = null;
            if (string.IsNullOrEmpty(p_fen)) throw new InvalidOperationException(msg_invalidFEN);
            int i_fen, i_piece;
            Coordinate2D _position = new Coordinate2D(1, rows);
            // set the pieces
            for (i_fen = 0; i_fen < p_fen.Length && _position.IsValid(); ++i_fen)
            {
                if (!_position.IsValid()) break;
                i_piece = _position.GetIndex();
                if (!i_piece.IsValid()) break;
                int skipCount;
                if (p_fen[i_fen] == '/') continue;
                if (char.IsLetter(p_fen[i_fen]))
                {
                    pieces[i_piece] = new ChessPiece(ChessNotation.GetPieceType(p_fen[i_fen]), ChessNotation.GetPieceColor(p_fen[i_fen]), i_piece.GetPosition());
                    skipCount = 1;
                }
                else if (char.IsDigit(p_fen[i_fen]) && int.TryParse(p_fen.AsSpan(i_fen, 1), out skipCount)) { }
                else if (i_piece != rows * columns - 1) throw new FormatException($"{msg_invalidFEN} : index = {i_fen} ");
                else break;
                if (_position.x + skipCount > columns) _position = new Coordinate2D(1, _position.y - 1);
                else _position = new Coordinate2D(_position.x + skipCount, _position.y);
            }
            if (++i_fen >= p_fen.Length) return;
            // set current color
            CurrentColor = ChessNotation.GetColorDirect(p_fen[i_fen]);
        }

        private void CountPieces()
        {
            PieceCount = 0;
            for (int i_p = 0; i_p < pieces.Length; ++i_p) if (pieces[i_p] != null) ++PieceCount;
            if (PieceCount <= 2) MatchStatus = ChessMatchStatus.InsuffecientMaterial;
        }

        private Coordinate2D? GetPositionOfKing(in ChessPieceColor p_color)
        {
            foreach (ChessPiece? _piece in pieces)
                if (_piece != null && _piece.type == ChessPieceType.King && _piece.color == p_color) return _piece.Position;
            return null;
        }

        internal bool IsPositionInCheck(in Coordinate2D p_position, in ChessPieceColor p_color)
        {
            if (!p_position.IsValid()) throw new InvalidOperationException(msg_positionInvalid);
            foreach (ChessMove _move in GetMovesRef(p_color.Inverse())) if (_move.self_to.EquivalentTo(p_position)) return true;
            return false;
        }

        private void FindIfColorInCheck(in ChessPieceColor p_color)
        {
            Coordinate2D? _kingPosition = GetPositionOfKing(p_color) ?? throw new InvalidOperationException($"{p_color} king not found");
            bool inCheck = IsPositionInCheck(_kingPosition.Value, p_color);
            switch (p_color)
            {
                case ChessPieceColor.White:
                    WhiteCheckedPosition = inCheck ? _kingPosition : null;
                    break;
                case ChessPieceColor.Black:
                    BlackCheckedPosition = inCheck ? _kingPosition : null;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal bool IsColorInCheck(in ChessPieceColor p_color)
        {
            return p_color switch
            {
                ChessPieceColor.White => WhiteCheckedPosition != null,
                ChessPieceColor.Black => BlackCheckedPosition != null,
                _ => throw new NotImplementedException(),
            };
        }

        private void MoveBoardPiece(in int p_index_from, in int p_index_to)
        {
            (pieces[p_index_to], pieces[p_index_from]) = (pieces[p_index_from], null);
            pieces[p_index_to]?.ChangePosition(p_index_to.GetPosition());
        }

        private bool IsUniqueInRow(in ChessPiece p_piece)
        {
            for (Coordinate2D _position = new Coordinate2D(p_piece.Position.x, 1); _position.IsValid();
                _position = new Coordinate2D(_position.x, _position.y + 1))
            {
                int i_piece = _position.GetIndex();
                if (pieces[i_piece] != null && pieces[i_piece]!.color == p_piece.color && pieces[i_piece]!.type == p_piece.type
                    && !_position.EquivalentTo(p_piece.Position)) return false;
            }
            return true;
        }

        private bool IsUniqueInColumn(in ChessPiece p_piece)
        {
            for (Coordinate2D _position = new Coordinate2D(1, p_piece.Position.y); _position.IsValid();
                _position = new Coordinate2D(_position.x + 1, _position.y))
            {
                int i_piece = _position.GetIndex();
                if (pieces[i_piece] != null && pieces[i_piece]!.color == p_piece.color && pieces[i_piece]!.type == p_piece.type
                    && !_position.EquivalentTo(p_piece.Position)) return false;
            }
            return true;
        }

        internal (bool p_error, string p_message) ApplyMove(in ChessMove p_move)
        {
            if (MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (MatchStatus != ChessMatchStatus.Running) throw new InvalidOperationException(msg_boardNotPlayable);
            if (!p_move.initialized) return (true, msg_moveInvalid);
            if (!p_move.self_from.IsValid()) return (true, $"{msg_moveInvalid} : self_from");
            if (!p_move.self_to.IsValid()) return (true, $"{msg_moveInvalid} : self_to");
            int i_self_from = p_move.self_from.GetIndex();
            int i_self_to = p_move.self_to.GetIndex();
            if (i_self_from == i_self_to) return (true, $"{msg_moveInvalid} : self_from == self_to");
            if (pieces[i_self_from] == null) return (true, $"{msg_moveInvalid} : no piece at self_from");

            ChessPiece _moveNotation_piece = pieces[i_self_from]!;
            bool _moveNotation_isCapture = pieces[i_self_to] != null;

            pieces[i_self_from]!.MarkAsLastMovedPiece(this);
            MoveBoardPiece(i_self_from, i_self_to);

            if (p_move.other_from.HasValue)
            {
                if (!p_move.other_from.Value.IsValid()) return (true, $"{msg_moveInvalid} : other_from");
                int i_other_from = p_move.other_from.Value.GetIndex();
                if (pieces[i_other_from] == null) return (true, $"{msg_moveInvalid} : no piece at other_from");

                if (p_move.other_to.HasValue)
                {
                    if (!p_move.other_to.Value.IsValid()) return (true, $"{msg_moveInvalid} : other_to");
                    // move piece at other_from to other_to - Castelling
                    int i_other_to = p_move.other_to.Value.GetIndex();
                    MoveBoardPiece(i_other_from, i_other_to);
                }
                else
                {
                    // remove the piece at other_from - Enpassant
                    pieces[i_other_from] = null;
                    _moveNotation_isCapture = true;
                }
            }

            // spawn required new piece at self_to - PawnPromotion
            if (p_move.spawn.HasValue && pieces[i_self_to] != null)
                pieces[i_self_to] = new ChessPiece(p_move.spawn.Value, pieces[i_self_to]!.color, p_move.self_to);

            if (GenerateMoveNotation)
            {
                moveNotation.Clear();
                if (p_move.other_from.HasValue && p_move.other_to.HasValue) moveNotation.Append(p_move.self_from.x < p_move.self_to.x
                    ? ChessNotation.kingSideCastelling : ChessNotation.queenSideCastelling);
                else
                {
                    if (_moveNotation_piece.type != ChessPieceType.Pawn) moveNotation.Append(ChessNotation.GetNotation(_moveNotation_piece.type));
                    bool _ambiguity = false;
                    foreach (ChessMove _move in GetMovesRef(_moveNotation_piece.color))
                        if (!_move.Equivalent_From(p_move) && pieces[_move.self_from.GetIndex()]!.type == _moveNotation_piece.type
                            && _move.Equivalent_To(p_move)) { _ambiguity = true; break; }
                    if (_ambiguity)
                    {
                        if (IsUniqueInColumn(_moveNotation_piece)) moveNotation.Append(ChessNotation.GetNotation(p_move.self_from, p_row: false));
                        else if (IsUniqueInRow(_moveNotation_piece)) moveNotation.Append(ChessNotation.GetNotation(p_move.self_from, p_column: false));
                        else moveNotation.Append(ChessNotation.GetNotation(p_move.self_from));
                    }
                    if (_moveNotation_isCapture)
                    {
                        if (!_ambiguity && _moveNotation_piece.type == ChessPieceType.Pawn) moveNotation.Append(ChessNotation.GetNotation(p_move.self_from, p_row: false));
                        moveNotation.Append(ChessNotation.capture);
                    }
                    moveNotation.Append(ChessNotation.GetNotation(p_move.self_to));
                    if (p_move.spawn.HasValue) moveNotation.Append(ChessNotation.spawn).Append(ChessNotation.GetNotation(p_move.spawn.Value));
                }
            }

            LastMovedPieceIndex = i_self_to;
            return (false, string.Empty);
        }

        private void FindPossibleMoves(in ChessPieceColor p_color)
        {
            List<ChessMove> _moves = GetMovesRef(p_color);
            _moves.Clear();
            for (int i_p = 0; i_p < pieces.Length; ++i_p)
                if (pieces[i_p] != null && pieces[i_p]!.color == p_color) _moves.AddRange(pieces[i_p]!.FindMoves(this));
        }

        private void IdentifyValidMoves(in ChessPieceColor p_color)
        {
            List<ChessMove> _movesToRemove = new List<ChessMove>(maxMoves);
            List<ChessMove> _moves = GetMovesRef(p_color);
            FindPossibleMoves(p_color);
            foreach (ChessMove _move in _moves)
            {
                ChessBoard _testBoard = CreateDeepClone();
                _testBoard.MatchStatus = ChessMatchStatus.Running;
                (bool error, _) = _testBoard.ApplyMove(_move);
                if (error) throw new InvalidOperationException(msg_moveNotApplicable);
                _testBoard.FindPossibleMoves(p_color.Inverse());
                _testBoard.FindIfColorInCheck(p_color);
                if (_testBoard.IsColorInCheck(p_color)) _movesToRemove.Add(_move);
            }
            foreach (ChessMove _move in _movesToRemove) _moves.Remove(_move);
            FindIfColorInCheck(p_color.Inverse());
        }

        private void CheckIfGameOver()
        {
            if (GetMovesRef(CurrentColor).Count == 0)
            {
                if (IsColorInCheck(CurrentColor))
                {
                    MatchStatus = CurrentColor switch
                    {
                        ChessPieceColor.White => ChessMatchStatus.WhiteCheckmated,
                        ChessPieceColor.Black => ChessMatchStatus.BlackCheckmated,
                        _ => throw new NotImplementedException(),
                    };
                }
                else MatchStatus = ChessMatchStatus.Stalemate;
            }
            if (GenerateMoveNotation)
            {
                if (MatchStatus == ChessMatchStatus.WhiteCheckmated || MatchStatus == ChessMatchStatus.BlackCheckmated) moveNotation.Append('#');
                else if (IsColorInCheck(CurrentColor)) moveNotation.Append('+');
            }
        }

        internal void StartTurn()
        {
            if (MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (MatchStatus != ChessMatchStatus.Paused) throw new InvalidOperationException(msg_turnInvalid);
            MatchStatus = ChessMatchStatus.Running;
        }

        internal void FinishTurn()
        {
            if (MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (MatchStatus != ChessMatchStatus.Running) throw new InvalidOperationException(msg_turnInvalid);
            MatchStatus = ChessMatchStatus.Paused;
            ++TurnCount;
            CountPieces();
            IdentifyValidMoves(CurrentColor);
            IdentifyValidMoves(CurrentColor.Inverse());
            CurrentColor = CurrentColor.Inverse();
            CheckIfGameOver();
        }

        internal void TimeoutTurn()
        {
            if (MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_boardNotConfigured);
            if (MatchStatus != ChessMatchStatus.Running) throw new InvalidOperationException(msg_turnInvalid);
            MatchStatus = CurrentColor switch
            {
                ChessPieceColor.White => ChessMatchStatus.WhiteTimedout,
                ChessPieceColor.Black => ChessMatchStatus.BlackTimedout,
                _ => throw new NotImplementedException(),
            };
        }

    }

}
