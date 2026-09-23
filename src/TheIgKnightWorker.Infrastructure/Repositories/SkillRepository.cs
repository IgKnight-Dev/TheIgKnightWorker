using TheIgKnightWorker.Infrastructure.DbModels;

namespace TheIgKnightWorker.Infrastructure.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly DapperContext DapperContext;
    private readonly IMapper Mapper;

    public SkillRepository(DapperContext dapperContext, IMapper mapper)
    {
        this.DapperContext = dapperContext;
        this.Mapper = mapper;
    }

    public async Task<List<Skill>> GetSkillsAsync()
    {
        var connection = this.DapperContext.Connection;

        var skills = await connection.QueryAsync<DbSkill>(PostgreSkillSql.GetSkills);

        return this.Mapper.Map<List<Skill>>(skills);
    }

    public async Task<Skill> GetSkillByIdAsync(short skillId)
    {
        var connection = this.DapperContext.Connection;
        var skill = await connection.QueryFirstOrDefaultAsync<DbSkill>(PostgreSkillSql.GetSkillById, new { Id = skillId });

        return this.Mapper.Map<Skill>(skill);
    }

    public async Task<IEnumerable<Skill>> GetSkillsByIdsAsync(IEnumerable<short> skillIds)
    {
        var ids = skillIds?.Distinct().ToArray();

        if (ids == null || ids.Length == 0)
        {
            return Enumerable.Empty<Skill>();
        }

        var connection = this.DapperContext.Connection;

        var skills = await connection.QueryAsync<DbSkill>(PostgreSkillSql.GetSkillsByIds, new { Ids = ids });

        return this.Mapper.Map<IEnumerable<Skill>>(skills);
    }

    public async Task<bool> SkillExistsAsync(string skillName)
    {
        var connection = this.DapperContext.Connection;
        var skill = await connection.QueryFirstOrDefaultAsync<int>(PostgreSkillSql.SkillExists, new { Name = skillName });

        return skill > 0 ? true : false;
    }

    public async Task<Skill> AddSkillAsync(Skill skill)
    {
        var connection = this.DapperContext.Connection;

        var dbSkill = this.Mapper.Map<DbSkill>(skill);

        var skillId = await connection.QuerySingleAsync<short>(
            PostgreSkillSql.AddSkill,
            dbSkill
        );

        skill.Id = skillId;

        return skill;
    }

    public async Task DeleteSkillAsync(short skillId)
    {
        var connection = this.DapperContext.Connection;
        await connection.ExecuteAsync(PostgreSkillSql.DeleteSkill, new { Id = skillId });
    }
}
