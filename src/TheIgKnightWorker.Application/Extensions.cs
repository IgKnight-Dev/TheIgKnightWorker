using System.ComponentModel;
using TheIgKnightWorker.Domain.Entities;

namespace TheIgKnightWorker.Application;

public static class Extensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());

        var attribute = field?
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        return attribute?.Description ?? value.ToString();
    }

    public static string BuildAchievementsSectionHtml(this PortfolioMetaData metaData, string templateKey, string achievements)
    {
        if (string.IsNullOrWhiteSpace(achievements))
        {
            return string.Empty;
        }

        return templateKey switch
        {
            "professional" =>
                $"""
            <div class="section">
                <div class="section-title">
                    Achievements
                </div>

                <div class="section-body">
                    {achievements}
                </div>
            </div>
            """,

            "experienced" =>
                $"""
            <section style="
                margin-top:32px;
                padding-top:24px;
                border-top:1px solid #dbe2ea;
            ">

                <div style="
                    margin-bottom:20px;
                ">

                    <div style="
                        color:#172033;
                        font-size:17pt;
                        font-weight:800;
                        line-height:1.2;
                        letter-spacing:-0.25px;
                    ">
                        Achievements
                    </div>

                    <div style="
                        width:46px;
                        height:3px;
                        margin-top:8px;
                        background:#2563eb;
                    "></div>

                </div>

                <div>
                    {achievements}
                </div>

            </section>
            """,

            "fresher" =>
                $"""
            <div class="section">

                <div class="section-title">
                    Achievements
                </div>

                <div class="section-body">
                    {achievements}
                </div>

            </div>
            """,

            "student" =>
                $"""
            <section class="section">

                <div class="section-title">
                    Achievements
                </div>

                <div class="section-body">
                    {achievements}
                </div>

            </section>
            """,

            _ =>
                $"""
            <div class="section">
                <div class="section-title">
                    Achievements
                </div>

                <div class="section-body">
                    {achievements}
                </div>
            </div>
            """
        };
    }
}
