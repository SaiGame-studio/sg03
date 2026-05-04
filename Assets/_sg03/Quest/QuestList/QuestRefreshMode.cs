namespace SG03.Quest
{
    public enum QuestRefreshMode
    {
        AssignAhead = 0,  // POST assign-ahead then return today's quests (default)
        TodayOnly   = 1,  // GET today's quests directly
    }
}
