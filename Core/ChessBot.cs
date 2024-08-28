namespace NMX.EzChess.Library.Core
{
    using System.Collections.Generic;
    using System.Text;
    using System;

    internal static class Evaluation
    {
        private const int
            k_infinity = 999999,
            k_recievedCheckPenalty = 1000,
            k_givenCheckReward = 600,
            k_capturedPieceMultiplier = 10,
            k_promotedPieceMultiplier = 3;

        private static readonly int[]
            s_pawnBias = {
              0,  0,  0,  0,  0,  0,  0,  0,
             50, 50, 50, 50, 50, 50, 50, 50,
             10, 10, 20, 30, 30, 20, 10, 10,
              5,  5, 10, 25, 25, 10,  5,  5,
              0,  0,  0, 20, 20,  0,  0,  0,
              5, -5,-10,  0,  0,-10, -5,  5,
              5, 10, 10,-20,-20, 10, 10,  5,
              0,  0,  0,  0,  0,  0,  0,  0,
            },
            s_knightBias = {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50,
            },
            s_bishopBias = {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20,
            },
            s_rookBias = {
              0,  0,  0,  0,  0,  0,  0,  0,
              5, 10, 10, 10, 10, 10, 10,  5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
              0,  0,  0,  5,  5,  0,  0,  0,
            },
            s_queenBias = {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20,
            },
            s_kingMiddleBias = {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20,
            },
            s_kingEndBias = {
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
                _ => throw new System.NotImplementedException(),
            };
        }

        private static int GetBoardValue(in ChessBoard p_board, in ChessPieceColor p_color)
        {
            if (p_board == null) throw new System.NullReferenceException();
            int playerValue = 0, enemyValue = 0;
            ChessPiece? piece;
            for (int i = 0; i < p_board.MaxPiecesCount; ++i)
            {
                piece = p_board.GetPieceAt(i);
                if (piece == null) continue;
                if (piece.color == p_color) playerValue += piece.type.GetValue();
                else enemyValue += piece.type.GetValue();
            }
            if (p_board.IsPlayerInCheck(p_color)) playerValue -= k_recievedCheckPenalty;
            if (p_board.IsPlayerInCheck(p_color.Inverse())) playerValue += k_givenCheckReward;
            return playerValue - enemyValue;
        }

        private static int GetBias(in ChessBoard p_board, in ChessPiece p_piece, in Coordinate2D p_position)
        {
            int index = (p_piece.color == ChessPieceColor.White ? p_position
                : new Coordinate2D(p_position.x, ChessBoard.ROWS + 1 - p_position.y)).GetIndex();
            return p_piece.type switch
            {
                ChessPieceType.Pawn => s_pawnBias[index],
                ChessPieceType.Rook => s_rookBias[index],
                ChessPieceType.Bishop => s_bishopBias[index],
                ChessPieceType.Knight => s_knightBias[index],
                ChessPieceType.Queen => s_queenBias[index],
                ChessPieceType.King => p_board.PieceCount >= 16 ? s_kingMiddleBias[index] : s_kingEndBias[index],
                _ => throw new NotImplementedException(),
            };
        }

        private static void SortMoves(in ChessBoard p_board, in List<ChessMove> p_moves)
        {
            int[] moveScores = new int[p_moves.Count];
            ChessPiece? ownPiece, capturePiece;
            for (int i = 0; i < p_moves.Count; ++i)
            {
                moveScores[i] = 0;
                ownPiece = p_board.GetPieceAt(p_moves[i].self_from.GetIndex());
                if (ownPiece == null) continue;
                capturePiece = p_board.GetPieceAt(p_moves[i].self_to.GetIndex());
                moveScores[i] += capturePiece == null 
                    ? GetBias(p_board, ownPiece, p_moves[i].self_to)
                    : k_capturedPieceMultiplier * capturePiece.type.GetValue() - ownPiece.type.GetValue();
                if (p_moves[i].spawn.HasValue) moveScores[i] += k_promotedPieceMultiplier * p_moves[i].spawn!.Value.GetValue();
            }
            int i_max;
            for (int i = 0; i < moveScores.Length - 1; ++i)
            {
                i_max = i;
                for (int j = i + 1; j < moveScores.Length; j++)
                    if (moveScores[j] > moveScores[i_max]) i_max = j;
                if (i_max != i)
                {
                    (p_moves[i_max], p_moves[i]) = (p_moves[i], p_moves[i_max]);
                    (moveScores[i_max], moveScores[i]) = (moveScores[i], moveScores[i_max]);
                }
            }
        }

        internal static (int p_value, ChessMove? p_move, int p_nodeCount) Search_MinMax(in ChessBoard p_board, in ChessPieceColor p_maxSide,
            in int p_depth, int p_alpha = -k_infinity, int p_beta = k_infinity)
        {
            if (p_depth == 0 || !p_board.Playable) return (GetBoardValue(p_board, p_maxSide), null, 1);
            SortMoves(p_board, p_board.GetMovesRef(p_board.CurrentColor));
            SortMoves(p_board, p_board.GetMovesRef(p_board.CurrentColor.Inverse()));
            bool isMaxTurn = p_board.CurrentColor == p_maxSide;
            int testValue, bestValue = isMaxTurn ? -k_infinity : k_infinity;
            ChessMove? bestMove = null;
            ChessBoard? testBoard;
            int totalNodeCount = 0, testNodeCount;
            foreach (ChessMove move in p_board.GetMovesRef(p_board.CurrentColor))
            {
                testBoard = p_board.CreateDeepClone() ?? throw new NullReferenceException();
                testBoard.ApplyMove(move);
                testBoard.FinishTurn();
                testBoard.StartTurn();
                (testValue, _, testNodeCount) = Search_MinMax(testBoard, p_maxSide, p_depth - 1, p_alpha, p_beta);
                totalNodeCount += testNodeCount;
                if (isMaxTurn)
                {
                    if (testValue > bestValue)
                    {
                        bestValue = testValue;
                        bestMove = move;
                    }
                    if (bestValue > p_alpha) p_alpha = bestValue;
                }
                else
                {
                    if (testValue < bestValue)
                    {
                        bestValue = testValue;
                        bestMove = move;
                    }
                    if (bestValue < p_beta) p_beta = bestValue;
                }
                if (p_beta <= p_alpha) break;
            }
            return (bestValue, bestMove, totalNodeCount);
        }
    }

    public static class ChessBot
    {
        private static readonly Random s_random;

        static ChessBot()
        {
            s_random = new Random();
        }

        public static ChessMove? GetRandomMove(in ChessBoard p_board, in ChessPieceColor p_side)
        {
            List<ChessMove> moves = p_board.GetMovesRef(p_side);
            if (moves.Count == 0) return null;
            int index = s_random.Next(moves.Count);
            return moves[index];
        }

        public static ChessMove? GetBestMove(in ChessBoard p_board, in ChessPieceColor p_side, in int p_depth)
        {
            (_, ChessMove? move, _) = Evaluation.Search_MinMax(p_board, p_side, p_depth);
            return move;
        }

        public static string SearchMove_Test(in ChessBoard p_board, in ChessPieceColor p_side, in int p_depth)
        {
            StringBuilder info = new StringBuilder($"  depth={p_depth}");
            (_, ChessMove? move, int nodeCount) = Evaluation.Search_MinMax(p_board, p_side, p_depth);
            if (move.HasValue) info.Append($"  | move:{ChessNotation.GetNotation(move.Value.self_from)}{ChessNotation.GetNotation(move.Value.self_to)}");
            else info.Append($"  | move:----");
            info.Append($"\t | nodes={nodeCount}");
            return info.ToString();
        }

    }

}
