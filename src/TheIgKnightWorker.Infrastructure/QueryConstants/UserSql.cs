namespace TheIgKnightWorker.Infrastructure.QueryConstants;

internal class UserSql
{
    public const string GetUserByUserId = @"
                        SELECT * FROM [User]
                        WHERE UserId = @UserId;
                    ";
}