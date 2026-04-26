namespace SG03.Quest
{
    public enum QuestRefreshMode
    {
        AssignAhead,  // POST assign-ahead then return today's quests (default)
        TodayOnly,    // GET today's quests directly
    }
}
