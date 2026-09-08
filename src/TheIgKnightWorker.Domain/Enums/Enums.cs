using System.ComponentModel;

namespace TheIgKnightWorker.Domain.Enums;

public class Enums
{
    public enum ResumeGenerationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }

    public enum PortfolioLinkType
    {
        [Description("Website")]
        Website = 1,
        [Description("LinkedIn")]
        LinkedIn = 2,
        [Description("Github")]
        Github = 3,
        [Description("Twitter")]
        Twitter = 4,
        [Description("LeetCode")]
        LeetCode = 5,
        [Description("HackerRank")]
        HackerRank = 6,
        [Description("Codeforces")]
        Codeforces = 7,
        [Description("StackOverflow")]
        StackOverflow = 8,
        [Description("Medium")]
        Medium = 9
    }

    public enum SkillCategory
    {
        Language = 1,

        Frontend = 2,

        Backend = 3,

        Database = 4,

        DevOps = 5,

        Cloud = 6,

        Mobile = 7,

        AI = 8,

        Testing = 9,

        Design = 10,

        Other = 99
    }

    public enum EventType
    {
        ResumeGeneration = 1,
    }

    public enum EventStatus
    {
        Created = 1,
        Queued = 2,
        InProgress = 3,
        Completed = 4,
        Failed = 5
    }
}
