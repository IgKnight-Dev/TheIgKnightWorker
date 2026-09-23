namespace TheIgKnightWorker.Infrastructure.PostgreQueryConstants;

internal class PostgreUserSql
{
    public const string GetUserByUserId = @"
        SELECT *
        FROM ""User""
        WHERE UserId = @UserId;";
}