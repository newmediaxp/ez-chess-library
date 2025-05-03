namespace NMX.EzChess.Console.Core;

using Library.Core;
using System;
using System.Text.RegularExpressions;

internal static partial class InputHandler
{
    public const string
        in_import = "import ",
        in_findm = "findm ",
        in_quit = "quit",
        in_start = "start",
        in_show = "show",
        in_export = "export",
        in_set = "set ",
        in_move = "move ",
        in_search_move = "search";
    private const string
        set_rotation = "rotation=",
        set_whiteIsBot = "whiteisbot=",
        set_blackIsBot = "blackisbot=";
    private static readonly Regex in_positionRegex;

    [GeneratedRegex(@"([a-z][1-9])")]
    private static partial Regex PositionRegex();

    static InputHandler()
    {
        in_positionRegex = PositionRegex();
    }

    public static bool HandleInput(in string? p_input)
    {
        EZChessManager.InputRecievedAck();
        if (string.IsNullOrEmpty(p_input)) return true;
        if (p_input.Length > in_import.Length && p_input.Contains(in_import, StringComparison.CurrentCultureIgnoreCase)) { EZChessManager.ImportBoard(p_input[in_import.Length..]); return true; }
        if (p_input.Length > in_findm.Length && p_input.Contains(in_findm, StringComparison.CurrentCultureIgnoreCase)) { EZChessManager.GetMoveFromNotation(p_input[in_findm.Length..]); return true; }
        if (p_input.Length == in_quit.Length && p_input.Contains(in_quit, StringComparison.CurrentCultureIgnoreCase)) { EZChessManager.Quit(); return true; }
        if (p_input.Length == in_start.Length && p_input.Contains(in_start, StringComparison.CurrentCultureIgnoreCase)) { EZChessManager.StartGame(); return true; }
        if (p_input.Length == in_show.Length && p_input.Contains(in_show, StringComparison.CurrentCultureIgnoreCase)) { EZChessManager.UpdateBoard(); return true; }
        if (p_input.Length == in_export.Length && p_input.Contains(in_export, StringComparison.CurrentCultureIgnoreCase)) { EZChessManager.ExportBoard(); return true; }
        if (p_input.Length > in_set.Length && p_input.Contains(in_set, StringComparison.CurrentCultureIgnoreCase))
        {
            bool _didSomething = false;
            string[] _arguments = p_input[in_set.Length..].Split(' ', ',');
            foreach (string _arg in _arguments)
            {
                if (_arg.Contains(set_rotation, StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(_arg.AsSpan(_arg.IndexOf(set_rotation) + set_rotation.Length), out bool value))
                { EZChessManager.Set(p_rotation: value);  _didSomething = true; }
                if (_arg.Contains(set_whiteIsBot, StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(_arg.AsSpan(_arg.IndexOf(set_whiteIsBot) + set_whiteIsBot.Length), out value))
                { EZChessManager.Set(p_whiteIsBot: value); _didSomething = true; }
                if (_arg.Contains(set_blackIsBot, StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(_arg.AsSpan(_arg.IndexOf(set_blackIsBot) + set_blackIsBot.Length), out value))
                { EZChessManager.Set(p_blackIsBot: value); _didSomething = true; }
            }
            return _didSomething;
        }
        if (p_input.Length > in_move.Length && p_input.Contains(in_move, StringComparison.CurrentCultureIgnoreCase))
        {
            MatchCollection _matches = in_positionRegex.Matches(p_input.ToLower()[in_move.Length..]);
            if (_matches.Count == 1 && _matches[0].Success && _matches[0].Groups.Count == 2)
            {
                //Console.WriteLine($"position ({matches[0].Groups[1].Value}");
                Coordinate2D? _position = ChessNotation.GetPosition(_matches[0].Groups[1].Value);
                if (!_position.HasValue) return false;
                EZChessManager.ShowMovesForCurrentPlayer(_position.Value);
                return true;
            }
            if (_matches.Count == 2 && _matches[0].Success && _matches[1].Success && _matches[0].Groups.Count == 2 && _matches[1].Groups.Count == 2)
            {
                //Console.WriteLine($"position ({matches[1].Groups[1].Value})");
                Coordinate2D? _pos_from = ChessNotation.GetPosition(_matches[0].Groups[1].Value),
                    _pos_to = ChessNotation.GetPosition(_matches[1].Groups[1].Value);
                if (!_pos_from.HasValue || !_pos_to.HasValue) return false;
                EZChessManager.ApplyMoveForCurrentPlayer(_pos_from.Value, _pos_to.Value);
                return true;
            }
            return false;
        }
        if (p_input.Length == in_search_move.Length && p_input.Contains(in_search_move, StringComparison.CurrentCultureIgnoreCase))
        { EZChessManager.SearchBestMoveTest(); return true; }
        return false;
    }

    public static void ProcessInput()
    {
        if (!HandleInput(Console.ReadLine())) EZChessManager.InputInvalid();
    }
}
