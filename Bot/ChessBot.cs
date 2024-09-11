namespace NMX.EzChess.Library.Bot
{
    using Core;
    using System.Collections.Generic;
    using System.Text;
    using System;

    internal static class ChessBot
    {
        private static readonly Random random;
        private const string
            msg_searchNotApplicable = "search not applicable";

        static ChessBot()
        {
            random = new Random();
        }

        public static ChessMove? GetRandomMove(in ChessBoard p_board, in ChessPieceColor p_side)
        {
            if (p_board.MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_searchNotApplicable);
            List<ChessMove> _moves = p_board.GetMovesRef(p_side);
            if (_moves.Count == 0) return null;
            int i_move = random.Next(_moves.Count);
            return _moves[i_move];
        }

        public static ChessMove? GetBestMove(in ChessBoard p_board, in ChessPieceColor p_side, in int p_depth)
        {
            if (p_board.MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_searchNotApplicable);
            (_, ChessMove? _move, _) = Evaluation.Search_MinMax(p_board, p_side, p_depth);
            return _move;
        }

        public static string AnalyzeBestMove(in ChessBoard p_board, in ChessPieceColor p_side, in int p_depth)
        {
            if (p_board.MatchStatus == ChessMatchStatus.NotConfigured) throw new InvalidOperationException(msg_searchNotApplicable);
            StringBuilder _info = new StringBuilder($"  depth={p_depth}");
            (_, ChessMove? _move, int _nodeCount) = Evaluation.Search_MinMax(p_board, p_side, p_depth);
            if (_move.HasValue) _info.Append($"  | move:{ChessNotation.GetNotation(_move.Value.self_from)}{ChessNotation.GetNotation(_move.Value.self_to)}");
            else _info.Append($"  | move:----");
            _info.Append($"\t | nodes={_nodeCount}");
            return _info.ToString();
        }

    }

}
