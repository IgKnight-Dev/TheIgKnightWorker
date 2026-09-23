using TheIgKnightWorker.Infrastructure.DbModels;

namespace TheIgKnightWorker.Infrastructure.Repositories;

public class ContextUserRepository : IContextUserRepository
{
    private readonly DapperContext DapperContext;
    private readonly IMapper Mapper;

    public ContextUserRepository(DapperContext dapperContext, IMapper mapper)
    {
        this.DapperContext = dapperContext;
        this.Mapper = mapper;
    }

    public async Task<User> GetUserByUserIdAsync(Guid userId)
    {
        var connection = this.DapperContext.Connection;
        var userDetails = await connection.QuerySingleOrDefaultAsync<DbUser>(PostgreUserSql.GetUserByUserId, new { UserId = userId }, this.DapperContext.CurrentTransaction);
        return this.Mapper.Map<User>(userDetails);
    }
}
