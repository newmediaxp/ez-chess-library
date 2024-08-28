namespace NMX.EzChess.Library.IO
{
    using Core;
    using System;
    using System.Text.RegularExpressions;

    internal static class InputHandlerCLI
    {
        public const string
            in_import = "import ",
            in_test_findm = "findm ",
            in_quit = "quit",
            in_start = "start",
            in_show = "show",
            in_export = "export",
            in_set = "set ",
            in_move = "move ",
            in_test_search_move = "search";
        private const string
            set_rotation = "rotation=",
            set_whiteIsBot = "whiteisbot=",
            set_blackIsBot = "blackisbot=";
        private static readonly Regex s_positionInputRegex;

        static InputHandlerCLI()
        {
            s_positionInputRegex = new Regex(@"([a-z][1-9])");
        }

        public static bool HandleInput(in string? p_input)
        {
            EZChessManagerCLI.H_InputRecievedAck();
            if (string.IsNullOrEmpty(p_input)) return true;
            if (p_input.Length > in_import.Length && p_input.Contains(in_import, StringComparison.CurrentCultureIgnoreCase)) { EZChessManagerCLI.H_ImportBoard(p_input[in_import.Length..]); return true; }
            if (p_input.Length > in_test_findm.Length && p_input.Contains(in_test_findm, StringComparison.CurrentCultureIgnoreCase)) { EZChessManagerCLI.H_Test_MoveFromNotation(p_input[in_test_findm.Length..]); return true; }
            if (p_input.Length == in_quit.Length && p_input.Contains(in_quit, StringComparison.CurrentCultureIgnoreCase)) { EZChessManagerCLI.H_Quit(); return true; }
            if (p_input.Length == in_start.Length && p_input.Contains(in_start, StringComparison.CurrentCultureIgnoreCase)) { EZChessManagerCLI.H_StartGame(); return true; }
            if (p_input.Length == in_show.Length && p_input.Contains(in_show, StringComparison.CurrentCultureIgnoreCase)) { EZChessManagerCLI.H_ShowBoard(); return true; }
            if (p_input.Length == in_export.Length && p_input.Contains(in_export, StringComparison.CurrentCultureIgnoreCase)) { EZChessManagerCLI.H_ExportBoard(); return true; }
            if (p_input.Length > in_set.Length && p_input.Contains(in_set, StringComparison.CurrentCultureIgnoreCase))
            {
                bool didSomething = false;
                string[] arguments = p_input[in_set.Length..].Split(' ', ',');
                foreach (string arg in arguments)
                {
                    if (arg.Contains(set_rotation, StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(arg.AsSpan(arg.IndexOf(set_rotation) + set_rotation.Length), out bool value))
                    { EZChessManagerCLI.H_Set(p_rotation: value);  didSomething = true; }
                    if (arg.Contains(set_whiteIsBot, StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(arg.AsSpan(arg.IndexOf(set_whiteIsBot) + set_whiteIsBot.Length), out value))
                    { EZChessManagerCLI.H_Set(p_whiteIsBot: value); didSomething = true; }
                    if (arg.Contains(set_blackIsBot, StringComparison.CurrentCultureIgnoreCase) && bool.TryParse(arg.AsSpan(arg.IndexOf(set_blackIsBot) + set_blackIsBot.Length), out value))
                    { EZChessManagerCLI.H_Set(p_blackIsBot: value); didSomething = true; }
                }
                return didSomething;
            }
            if (p_input.Length > in_move.Length && p_input.Contains(in_move, StringComparison.CurrentCultureIgnoreCase))
            {
                MatchCollection matches = s_positionInputRegex.Matches(p_input.ToLower()[in_move.Length..]);
                if (matches.Count == 1 && matches[0].Success && matches[0].Groups.Count == 2)
                {
                    //Console.WriteLine($"position ({matches[0].Groups[1].Value}");
                    Coordinate2D? position = ChessNotation.GetPosition(matches[0].Groups[1].Value);
                    if (!position.HasValue) return false;
                    EZChessManagerCLI.H_ShowMovesForCurrentPlayer(position.Value);
                    return true;
                }
                if (matches.Count == 2 && matches[0].Success && matches[1].Success && matches[0].Groups.Count == 2 && matches[1].Groups.Count == 2)
                {
                    //Console.WriteLine($"position ({matches[1].Groups[1].Value})");
                    Coordinate2D? pos_from = ChessNotation.GetPosition(matches[0].Groups[1].Value), pos_to = ChessNotation.GetPosition(matches[1].Groups[1].Value);
                    if (!pos_from.HasValue || !pos_to.HasValue) return false;
                    EZChessManagerCLI.H_ApplyMoveForCurrentPlayer(pos_from.Value, pos_to.Value);
                    return true;
                }
                return false;
            }
            if (p_input.Length == in_test_search_move.Length && p_input.Contains(in_test_search_move, StringComparison.CurrentCultureIgnoreCase))
            { EZChessManagerCLI.H_Test_SearchMove(); return true; }
            return false;
        }

        public static void ProcessInputs()
        {
            Console.Write("> ");
            if (!HandleInput(Console.ReadLine())) EZChessManagerCLI.H_InputInvalid();
        }

    }
}
