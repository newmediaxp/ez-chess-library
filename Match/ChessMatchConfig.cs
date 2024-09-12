namespace NMX.EzChess.Library.Match
{
    public struct ChessMatchConfig
    {
        public bool whiteIsBot, blackIsBot;
        public int clockRate, minBotDelay;
        public int totalTimeLimit, moveTimeLimit;
    }
}
