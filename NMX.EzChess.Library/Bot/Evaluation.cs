namespace NMX.EzChess.Library.Bot
{
    using Core;
    using System.Collections.Generic;
    using System;

    internal static class Evaluation
    {
        private const int
            infinity = 999999,
            recievedCheckPenalty = 1000,
            givenCheckReward = 600,
            capturedPieceMultiplier = 10,
            promotedPieceMultiplier = 3,
            kingMiddleBiasMinPieces = 16;

        private static readonly int[]
            pawnBias = {
              0,  0,  0,  0,  0,  0,  0,  0,
             50, 50, 50, 50, 50, 50, 50, 50,
             10, 10, 20, 30, 30, 20, 10, 10,
              5,  5, 10, 25, 25, 10,  5,  5,
              0,  0,  0, 20, 20,  0,  0,  0,
              5, -5,-10,  0,  0,-10, -5,  5,
              5, 10, 10,-20,-20, 10, 10,  5,
              0,  0,  0,  0,  0,  0,  0,  0,
            },
            knightBias = {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50,
            },
            bishopBias = {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20,
            },
            rookBias = {
              0,  0,  0,  0,  0,  0,  0,  0,
              5, 10, 10, 10, 10, 10, 10,  5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
              0,  0,  0,  5,  5,  0,  0,  0,
            },
            queenBias = {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20,
            },
            kingMiddleBias = {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20,
            },
            kingEndBias = {
            -50,-40,-30,-20,-20,-30,-40,-50,
            -30,-20,-10,  0,  0,-10,-20,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-30,  0,  0,  0,  0,-30,-30,
            -50,-30,-30,-30,-30,-30,-30,-50,
            };

        private static int GetValue(this ChessPieceType p_type)
        {
            return p_type switch
            {
                ChessPieceType.Pawn => 100,
                ChessPieceType.Rook => 400,
                ChessPieceType.Bishop => 400,
                ChessPieceType.Knight => 500,
                ChessPieceType.Queen => 900,
                ChessPieceType.King => 5000,
                _ => throw new NotImplementedException(),
            };
        }

        private static int GetBoardValue(in ChessBoard p_board, in ChessPieceColor p_color)
        {
            if (p_board == null) throw new NullReferenceException();
            int _playerValue = 0, _enemyValue = 0;
            ChessPiece? piece;
            for (int i = 0; i < ChessBoard.maxPiecesCount; ++i)
            {
                piece = p_board.GetPieceAt(i);
                if (piece == null) continue;
                if (piece.color == p_color) _playerValue += piece.type.GetValue();
                else _enemyValue += piece.type.GetValue();
            }
            if (p_board.IsColorInCheck(p_color)) _playerValue -= recievedCheckPenalty;
            if (p_board.IsColorInCheck(p_color.Inverse())) _playerValue += givenCheckReward;
            return _playerValue - _enemyValue;
        }

        private static int GetBias(in ChessBoard p_board, in ChessPiece p_piece, in Coordinate2D p_position)
        {
            int i_piece = (p_piece.color == ChessPieceColor.White ? p_position
                : new Coordinate2D(p_position.x, ChessBoard.rows + 1 - p_position.y)).GetIndex();
            return p_piece.type switch
            {
                ChessPieceType.Pawn => pawnBias[i_piece],
                ChessPieceType.Rook => rookBias[i_piece],
                ChessPieceType.Bishop => bishopBias[i_piece],
                ChessPieceType.Knight => knightBias[i_piece],
                ChessPieceType.Queen => queenBias[i_piece],
                ChessPieceType.King => p_board.PieceCount >= kingMiddleBiasMinPieces ? kingMiddleBias[i_piece] : kingEndBias[i_piece],
                _ => throw new NotImplementedException(),
            };
        }

        private static void SortMoves(in ChessBoard p_board, in List<ChessMove> p_moves)
        {
            int[] _moveScores = new int[p_moves.Count];
            ChessPiece? _ownPiece, _capturePiece;
            for (int i = 0; i < p_moves.Count; ++i)
            {
                _moveScores[i] = 0;
                _ownPiece = p_board.GetPieceAt(p_moves[i].self_from.GetIndex());
                if (_ownPiece == null) continue;
                _capturePiece = p_board.GetPieceAt(p_moves[i].self_to.GetIndex());
                _moveScores[i] += GetBias(p_board, _ownPiece, p_moves[i].self_to);
                if (_capturePiece != null) _moveScores[i] += capturedPieceMultiplier * _capturePiece.type.GetValue() - _ownPiece.type.GetValue();
                if (p_moves[i].spawn.HasValue) _moveScores[i] += promotedPieceMultiplier * p_moves[i].spawn!.Value.GetValue();
            }
            int _maxScore;
            for (int i_score = 0; i_score < _moveScores.Length - 1; ++i_score)
            {
                _maxScore = i_score;
                for (int j_score = i_score + 1; j_score < _moveScores.Length; j_score++)
                    if (_moveScores[j_score] > _moveScores[_maxScore]) _maxScore = j_score;
                if (_maxScore != i_score)
                {
                    (p_moves[_maxScore], p_moves[i_score]) = (p_moves[i_score], p_moves[_maxScore]);
                    (_moveScores[_maxScore], _moveScores[i_score]) = (_moveScores[i_score], _moveScores[_maxScore]);
                }
            }
        }

        public static (int p_value, ChessMove? p_move, int p_nodeCount) Search_MinMax(in ChessBoard p_board, in ChessPieceColor p_maxSide,
            in int p_depth, int p_alpha = -infinity, int p_beta = infinity)
        {
            if (p_depth == 0 || p_board.MatchStatus != ChessMatchStatus.Running) return (GetBoardValue(p_board, p_maxSide), null, 1);
            SortMoves(p_board, p_board.GetMovesRef(p_board.CurrentColor));
            SortMoves(p_board, p_board.GetMovesRef(p_board.CurrentColor.Inverse()));
            bool _isMaxTurn = p_board.CurrentColor == p_maxSide;
            int _testValue, _bestValue = _isMaxTurn ? -infinity : infinity;
            ChessMove? _bestMove = null;
            ChessBoard _testBoard;
            int _totalNodeCount = 0, _testNodeCount;
            foreach (ChessMove _move in p_board.GetMovesRef(p_board.CurrentColor))
            {
                _testBoard = p_board.CreateDeepClone();
                _testBoard.ApplyMove(_move);
                _testBoard.FinishTurn();
                if (_testBoard.MatchStatus != ChessMatchStatus.Paused) return (GetBoardValue(p_board, p_maxSide), null, 1);
                _testBoard.StartTurn();
                (_testValue, _, _testNodeCount) = Search_MinMax(_testBoard, p_maxSide, p_depth - 1, p_alpha, p_beta);
                _totalNodeCount += _testNodeCount;
                if (_isMaxTurn)
                {
                    if (_testValue > _bestValue)
                    {
                        _bestValue = _testValue;
                        _bestMove = _move;
                    }
                    if (_bestValue > p_alpha) p_alpha = _bestValue;
                }
                else
                {
                    if (_testValue < _bestValue)
                    {
                        _bestValue = _testValue;
                        _bestMove = _move;
                    }
                    if (_bestValue < p_beta) p_beta = _bestValue;
                }
                if (p_beta <= p_alpha) break;
            }
            return (_bestValue, _bestMove, _totalNodeCount);
        }
    }

}
