namespace NMX.EzChess.Library.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public sealed class ChessBoard
    {
        public const int ROWS = 8, COLUMNS = 8;
        private readonly ChessPiece?[] m_pieces;
        private readonly int m_maxMoves;
        private readonly List<ChessMove> m_whiteMoves, m_blackMoves;
        private readonly StringBuilder m_moveNotation;
        private const string  k_defaultFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        private const char
            k_mvn_capture = 'x',
            k_mvn_spawn = '=',
            k_mvn_check = '+',
            k_mvn_checkmate = '#';
        private const string
            k_mvn_kingSideCastelling = "0-0",
            k_mvn_queenSideCastelling = "0-0-0";
        private const string
            k_msg_boardNotConfigured = "board not configured",
            k_msg_boardNotPlayable = "board not playable",
            k_msg_positionInvalid = "position invalid",
            k_msg_moveInvalid = "move invalid",
            k_msg_moveNotApplicable = "move not applicable",
            k_msg_invalidFEN = "invalid FEN";

        public bool Configured { get; private set; }
        public bool Playable { get; private set; }
        public ChessMatchEndStatus EndStatus { get; private set; }
        public int TurnCount { get; private set; }
        public int PieceCount { get; private set; }
        public int MaxPiecesCount => ROWS * COLUMNS;
        public int LastMovedPieceIndex { get; private set; }
        public ChessPieceColor CurrentColor { get; private set; }
        public Coordinate2D? WhiteCheckedPosition { get; private set; }
        public Coordinate2D? BlackCheckedPosition { get; private set; }
        public string FEN { get => GetFEN(); set => SetFEN(value); }
        public bool GenerateMoveNotation { get; set; }
        public string LastMoveNotation { get => m_moveNotation.ToString(); }


        public ChessBoard()
        {
            m_pieces = new ChessPiece[ROWS * COLUMNS];
            m_maxMoves = 2 * 8 * ChessPieceType.Pawn.GetMaxMoves()
                + 2 * 2 * ChessPieceType.Rook.GetMaxMoves()
                + 2 * 2 * ChessPieceType.Knight.GetMaxMoves()
                + 2 * 2 * ChessPieceType.Bishop.GetMaxMoves()
                + 2 * ChessPieceType.Queen.GetMaxMoves()
                + 2 * ChessPieceType.King.GetMaxMoves();
            m_whiteMoves = new List<ChessMove>(m_maxMoves);
            m_blackMoves = new List<ChessMove>(m_maxMoves);
            GenerateMoveNotation = false;
            m_moveNotation = new StringBuilder();
        }

        public void ConfigureBoard(in string? p_fen = null)
        {
            Playable = false;
            EndStatus = ChessMatchEndStatus.None;
            TurnCount = 0;
            PieceCount = 0;
            LastMovedPieceIndex = -1;
            CurrentColor = ChessPieceColor.White;
            WhiteCheckedPosition = null;
            BlackCheckedPosition = null;
            m_whiteMoves.Clear();
            m_blackMoves.Clear();
            if (!string.IsNullOrEmpty(p_fen)) SetFEN(p_fen);
            else SetFEN(k_defaultFEN);
            CountPieces();
            Configured = true;
        }

        public ChessBoard CreateDeepClone()
        {
            if (!Configured) throw new InvalidOperationException(k_msg_boardNotConfigured);
            ChessBoard board = new ChessBoard()
            {
                Configured = Configured,
                Playable = Playable,
                EndStatus = EndStatus,
                TurnCount = TurnCount,
                PieceCount = PieceCount,
                LastMovedPieceIndex = LastMovedPieceIndex,
                CurrentColor = CurrentColor,
                WhiteCheckedPosition = WhiteCheckedPosition,
                BlackCheckedPosition = BlackCheckedPosition,
            };
            for (int i = 0; i < m_pieces.Length; ++i) board.m_pieces[i] = m_pieces[i]?.CreateDeepClone();
            return board;
        }

        public ChessPiece? GetPieceAt(in int p_index) => p_index.IsValid() ? m_pieces[p_index] : null;

        public List<ChessMove> GetMovesRef(in ChessPieceColor p_color) => p_color switch
        {
            ChessPieceColor.White => m_whiteMoves,
            ChessPieceColor.Black => m_blackMoves,
            _ => throw new NotImplementedException(),
        };

        public List<ChessMove>? GetMovesAt(in ChessPieceColor p_color, in Coordinate2D p_position)
        {
            if (!Configured) throw new InvalidOperationException(k_msg_boardNotConfigured);
            if (!p_position.IsValid() || m_pieces[p_position.GetIndex()]?.color != CurrentColor) return null;
            List<ChessMove> moves = new List<ChessMove>(m_pieces[p_position.GetIndex()]!.type.GetMaxMoves());
            foreach (ChessMove move in GetMovesRef(p_color))
                if (move.self_from.EquivalentTo(p_position)) moves.Add(move);
            return moves;
        }

        private string GetFEN()
        {
            StringBuilder fen = new StringBuilder();
            int skipCount = 0;
            // get the pieces
            for (Coordinate2D position = new Coordinate2D(1, ROWS); position.IsValid();
                position = position.x < COLUMNS ? new Coordinate2D(position.x + 1, position.y) : new Coordinate2D(1, position.y - 1))
            {
                int i_piece = position.GetIndex();
                if (!i_piece.IsValid()) break;
                if (position.y < ROWS && position.x == 1)
                {
                    if (skipCount > 0) fen.Append(skipCount);
                    skipCount = 0;
                    fen.Append('/');
                }
                if (m_pieces[i_piece] == null) { ++skipCount; continue; }
                if (skipCount > 0) fen.Append(skipCount);
                skipCount = 0;
                char piece_letter = ChessNotation.GetNotation(m_pieces[i_piece]!.type);
                fen.Append(m_pieces[i_piece]!.color == ChessPieceColor.White ? piece_letter : char.ToLower(piece_letter));
            }
            // get current color
            fen.Append($" {ChessNotation.GetNotation(CurrentColor)} ");
            return fen.ToString();
        }

        private void SetFEN(in string p_fen)
        {
            for (int i = 0; i < m_pieces.Length; ++i) m_pieces[i] = null;
            if (string.IsNullOrEmpty(p_fen)) throw new InvalidOperationException(k_msg_invalidFEN);
            int i_fen, i_piece;
            Coordinate2D position = new Coordinate2D(1, ROWS);
            // set the pieces
            for (i_fen = 0; i_fen < p_fen.Length && position.IsValid(); ++i_fen)
            {
                if (!position.IsValid()) break;
                i_piece = position.GetIndex();
                if (!i_piece.IsValid()) break;
                int skipCount;
                if (p_fen[i_fen] == '/') continue;
                if (char.IsLetter(p_fen[i_fen]))
                {
                    m_pieces[i_piece] = new ChessPiece(ChessNotation.GetPieceType(p_fen[i_fen]), ChessNotation.GetPieceColor(p_fen[i_fen]), i_piece.GetPosition());
                    skipCount = 1;
                }
                else if (char.IsDigit(p_fen[i_fen]) && int.TryParse(p_fen.AsSpan(i_fen, 1), out skipCount)) { }
                else if (i_piece != ROWS * COLUMNS - 1) throw new FormatException($"{k_msg_invalidFEN} : index = {i_fen} ");
                else break;
                if (position.x + skipCount > COLUMNS) position = new Coordinate2D(1, position.y - 1);
                else position = new Coordinate2D(position.x + skipCount, position.y);
            }
            if (++i_fen >= p_fen.Length) return;
            // set current color
            CurrentColor = ChessNotation.GetColorDirect(p_fen[i_fen]);
        }

        private void CountPieces()
        {
            PieceCount = 0;
            for (int i = 0; i < m_pieces.Length; ++i) if (m_pieces[i] != null) ++PieceCount;
            if (PieceCount <= 2) EndStatus = ChessMatchEndStatus.InsuffecientMaterial;
        }

        private Coordinate2D? GetPositionOfKing(in ChessPieceColor p_color)
        {
            foreach (ChessPiece? piece in m_pieces)
                if (piece != null && piece.type == ChessPieceType.King && piece.color == p_color) return piece.Position;
            return null;
        }

        public bool IsPositionInCheck(in Coordinate2D p_position, in ChessPieceColor p_color)
        {
            if (!Configured) throw new InvalidOperationException(k_msg_boardNotConfigured);
            if (!p_position.IsValid()) throw new InvalidOperationException(k_msg_positionInvalid);
            foreach (ChessMove move in GetMovesRef(p_color.Inverse())) if (move.self_to.EquivalentTo(p_position)) return true;
            return false;
        }

        private void FindIfPlayerInCheck(in ChessPieceColor p_color)
        {
            Coordinate2D? kingPosition = GetPositionOfKing(p_color) ?? throw new InvalidOperationException($"{p_color} king not found");
            bool inCheck = IsPositionInCheck(kingPosition.Value, p_color);
            switch (p_color)
            {
                case ChessPieceColor.White:
                    WhiteCheckedPosition = inCheck ? kingPosition : null;
                    break;
                case ChessPieceColor.Black:
                    BlackCheckedPosition = inCheck ? kingPosition : null;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public bool IsPlayerInCheck(in ChessPieceColor p_color)
        {
            if (!Configured) throw new InvalidOperationException(k_msg_boardNotConfigured);
            return p_color switch
            {
                ChessPieceColor.White => WhiteCheckedPosition != null,
                ChessPieceColor.Black => BlackCheckedPosition != null,
                _ => throw new NotImplementedException(),
            };
        }

        private void MoveBoardPiece(in int p_index_from, in int p_index_to)
        {
            (m_pieces[p_index_to], m_pieces[p_index_from]) = (m_pieces[p_index_from], null);
            m_pieces[p_index_to]?.ChangePosition(p_index_to.GetPosition());
        }

        private bool IsUniqueInRow(in ChessPiece p_piece)
        {
            for (Coordinate2D position = new Coordinate2D(p_piece.Position.x, 1); position.IsValid();
                position = new Coordinate2D(position.x, position.y + 1))
            {
                int i = position.GetIndex();
                if (m_pieces[i] != null && m_pieces[i]!.color == p_piece.color && m_pieces[i]!.type == p_piece.type
                    && !position.EquivalentTo(p_piece.Position)) return false;
            }
            return true;
        }

        private bool IsUniqueInColumn(in ChessPiece p_piece)
        {
            for (Coordinate2D position = new Coordinate2D(1, p_piece.Position.y); position.IsValid();
                position = new Coordinate2D(position.x + 1, position.y))
            {
                int i = position.GetIndex();
                if (m_pieces[i] != null && m_pieces[i]!.color == p_piece.color && m_pieces[i]!.type == p_piece.type
                    && !position.EquivalentTo(p_piece.Position)) return false;
            }
            return true;
        }

        public ChessMove? GetChessMoveFromMoveNotation(in string p_moveNotation)
        {
            if (string.IsNullOrEmpty(p_moveNotation) || p_moveNotation.Length < 2) return null;
            List<ChessMove> moves = GetMovesRef(CurrentColor);
            try
            {
                if (p_moveNotation == k_mvn_kingSideCastelling) return moves.Find(move => move.other_from.HasValue && move.other_to.HasValue && move.self_from.x < move.self_to.x);
                if (p_moveNotation == k_mvn_queenSideCastelling) return moves.Find(move => move.other_from.HasValue && move.other_to.HasValue && move.self_from.x > move.self_to.x);
                ChessPieceType type = ChessNotation.GetPieceType(p_moveNotation[0]);
                int i_from, i_to, row = -1, column = -1, i_x = p_moveNotation.IndexOf(k_mvn_capture);
                for (i_to = p_moveNotation.Length - 1; i_to >= 0 && i_to > i_x; --i_to)
                {
                    if (p_moveNotation[i_to] == k_mvn_spawn || p_moveNotation[i_to] == k_mvn_check || p_moveNotation[i_to] == k_mvn_checkmate) continue;
                    if (row == -1 && char.IsDigit(p_moveNotation[i_to]))
                    { row = int.Parse(p_moveNotation.AsSpan(i_to, 1)); continue; }
                    if (column == -1 && char.IsLetter(p_moveNotation[i_to]) && char.IsLower(p_moveNotation[i_to]))
                    { column = p_moveNotation[i_to] - 96; continue; }
                    if (row != -1 && column != -1) break;
                }
                Coordinate2D self_to = new Coordinate2D(column, row);
                if (!self_to.IsValid()) return null;
                row = -1; column = -1;
                for (i_from = 0; i_from < p_moveNotation.Length && i_from <= i_to; ++i_from)
                {
                    if (i_from == 0 && type != ChessPieceType.Pawn) continue;
                    if (i_x != -1 && i_from >= i_x) break;
                    if (row == -1 && char.IsDigit(p_moveNotation[i_from]))
                    { row = int.Parse(p_moveNotation.AsSpan(i_from, 1)); continue; }
                    if (column == -1 && char.IsLetter(p_moveNotation[i_from]) && char.IsLower(p_moveNotation[i_from]))
                    { column = p_moveNotation[i_from] - 96; continue; }
                }
                if (column == -1 && row == -1)
                    return moves.Find(move => move.self_to.EquivalentTo(self_to) && m_pieces[move.self_from.GetIndex()]!.type == type);
                if (column == -1)
                    return moves.Find(move => move.self_to.EquivalentTo(self_to) && m_pieces[move.self_from.GetIndex()]!.type == type
                    && move.self_from.y == row);
                if (row == -1)
                    return moves.Find(move => move.self_to.EquivalentTo(self_to) && m_pieces[move.self_from.GetIndex()]!.type == type
                    && move.self_from.x == column);
                return moves.Find(move => move.self_to.EquivalentTo(self_to) && m_pieces[move.self_from.GetIndex()]!.type == type
                    && move.self_from.x == column && move.self_from.y == row);
            }
            catch { return null; }
        }

        public (bool p_success, string p_message) ApplyMove(in ChessMove p_move)
        {
            if (!Configured) throw new InvalidOperationException(k_msg_boardNotConfigured);
            if (!Playable) throw new InvalidOperationException(k_msg_boardNotPlayable);
            if (!p_move.initialized) return (false, k_msg_moveInvalid);
            if (!p_move.self_from.IsValid()) return (false, $"{k_msg_moveInvalid} : self_from");
            if (!p_move.self_to.IsValid()) return (false, $"{k_msg_moveInvalid} : self_to");
            int i_self_from = p_move.self_from.GetIndex();
            int i_self_to = p_move.self_to.GetIndex();
            if (i_self_from == i_self_to) return (false, $"{k_msg_moveInvalid} : self_from == self_to");
            if (m_pieces[i_self_from] == null) return (false, $"{k_msg_moveInvalid} : no piece at self_from");

            ChessPiece moveNotation_piece = m_pieces[i_self_from]!;
            bool moveNotation_isCapture = m_pieces[i_self_to] != null;

            // perform normal move procedures
            // for (int i = 0; i < m_pieces.Length; ++i) if (m_pieces[i] != null) m_pieces[i]!.m_lastMovedPiece = i == i_self_from;
            m_pieces[i_self_from]!.MarkAsLastMovedPiece(this);
            MoveBoardPiece(i_self_from, i_self_to);

            if (p_move.other_from.HasValue)
            {
                if (!p_move.other_from.Value.IsValid()) return (false, $"{k_msg_moveInvalid} : other_from");
                int i_other_from = p_move.other_from.Value.GetIndex();
                if (m_pieces[i_other_from] == null) return (false, $"{k_msg_moveInvalid} : no piece at other_from");

                if (p_move.other_to.HasValue)
                {
                    if (!p_move.other_to.Value.IsValid()) return (false, $"{k_msg_moveInvalid} : other_to");
                    // move piece at other_from to other_to - Castelling
                    int i_other_to = p_move.other_to.Value.GetIndex();
                    MoveBoardPiece(i_other_from, i_other_to);
                }
                else
                {
                    // remove the piece at other_from - Enpassant
                    m_pieces[i_other_from] = null;
                    moveNotation_isCapture = true;
                }
            }

            // spawn required new piece - PawnPromotion
            if (p_move.spawn.HasValue && m_pieces[i_self_to] != null)
                m_pieces[i_self_to] = new ChessPiece(p_move.spawn.Value, m_pieces[i_self_to]!.color, p_move.self_to);

            if (GenerateMoveNotation)
            {
                m_moveNotation.Clear();
                if (p_move.other_from.HasValue && p_move.other_to.HasValue) m_moveNotation.Append(p_move.self_from.x < p_move.self_to.x 
                    ? k_mvn_kingSideCastelling : k_mvn_queenSideCastelling);
                else
                {
                    if (moveNotation_piece.type != ChessPieceType.Pawn) m_moveNotation.Append(ChessNotation.GetNotation(moveNotation_piece.type));
                    bool ambiguity = false;
                    foreach (ChessMove move in GetMovesRef(moveNotation_piece.color))
                        if (!move.EquivalentTo_SelfFrom(p_move) && m_pieces[move.self_from.GetIndex()]!.type == moveNotation_piece.type
                            && move.Equivalent_To(p_move)) { ambiguity = true; break; }
                    if (ambiguity)
                    {
                        if (IsUniqueInColumn(moveNotation_piece)) m_moveNotation.Append(ChessNotation.GetNotation(p_move.self_from, p_row: false));
                        else if (IsUniqueInRow(moveNotation_piece)) m_moveNotation.Append(ChessNotation.GetNotation(p_move.self_from, p_column: false));
                        else m_moveNotation.Append(ChessNotation.GetNotation(p_move.self_from));
                    }
                    if (moveNotation_isCapture)
                    {
                        if (!ambiguity && moveNotation_piece.type == ChessPieceType.Pawn) m_moveNotation.Append(ChessNotation.GetNotation(p_move.self_from, p_row: false));
                        m_moveNotation.Append(k_mvn_capture);
                    }
                    m_moveNotation.Append(ChessNotation.GetNotation(p_move.self_to));
                    if (p_move.spawn.HasValue) m_moveNotation.Append(k_mvn_spawn).Append(ChessNotation.GetNotation(p_move.spawn.Value));
                }
            }

            return (true, string.Empty);
        }

        private void FindPossibleMoves(in ChessPieceColor p_color)
        {
            List<ChessMove> moves = GetMovesRef(p_color);
            moves.Clear();
            for (int i = 0; i < m_pieces.Length; ++i)
                if (m_pieces[i] != null && m_pieces[i]!.color == p_color) moves.AddRange(m_pieces[i]!.FindMoves(this));
        }

        private void IdentifyValidMoves(in ChessPieceColor p_color)
        {
            List<ChessMove> movesToRemove = new List<ChessMove>(m_maxMoves);
            List<ChessMove> moves = GetMovesRef(p_color);
            FindPossibleMoves(p_color);
            foreach (ChessMove move in moves)
            {
                ChessBoard testBoard = CreateDeepClone();
                testBoard.Playable = true;
                (bool success, _) = testBoard.ApplyMove(move);
                if (!success) throw new InvalidOperationException(k_msg_moveNotApplicable);
                testBoard.FindPossibleMoves(p_color.Inverse());
                testBoard.FindIfPlayerInCheck(p_color);
                if (testBoard.IsPlayerInCheck(p_color)) movesToRemove.Add(move);
            }
            foreach (ChessMove move in movesToRemove) moves.Remove(move);
            FindIfPlayerInCheck(p_color.Inverse());
        }

        private void CheckIfGameOver()
        {
            if (GetMovesRef(CurrentColor).Count == 0)
            {
                if (IsPlayerInCheck(CurrentColor))
                {
                    EndStatus = CurrentColor switch
                    {
                        ChessPieceColor.White => ChessMatchEndStatus.WhiteCheckmated,
                        ChessPieceColor.Black => ChessMatchEndStatus.BlackCheckmated,
                        _ => throw new NotImplementedException(),
                    };
                }
                else EndStatus = ChessMatchEndStatus.Stalemate;
            }
            if (!GenerateMoveNotation)
            {
                if (EndStatus == ChessMatchEndStatus.WhiteCheckmated || EndStatus == ChessMatchEndStatus.BlackCheckmated) m_moveNotation.Append('#');
                else if (IsPlayerInCheck(CurrentColor)) m_moveNotation.Append('+');
            }
        }

        public void StartTurn()
        {
            if (!Configured) throw new InvalidOperationException(k_msg_boardNotConfigured);
            if (TurnCount == 0)
            {
                IdentifyValidMoves(CurrentColor.Inverse());
                IdentifyValidMoves(CurrentColor);
                CheckIfGameOver();
            }
            if (GetMovesRef(CurrentColor).Count > 0) Playable = true;
        }

        public void FinishTurn()
        {
            if (!Configured) throw new InvalidOperationException(k_msg_boardNotConfigured);
            Playable = false;
            ++TurnCount;
            CountPieces();
            IdentifyValidMoves(CurrentColor);
            IdentifyValidMoves(CurrentColor.Inverse());
            CurrentColor = CurrentColor.Inverse();
            CheckIfGameOver();
        }

    }

}
