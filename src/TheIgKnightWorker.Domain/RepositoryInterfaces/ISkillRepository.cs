namespace TheIgKnightWorker.Domain.RepositoryInterfaces;

public interface ISkillRepository
{
    Task<List<Skill>> GetSkillsAsync();

    Task<Skill?> GetSkillByIdAsync(short id);

    Task<IEnumerable<Skill>> GetSkillsByIdsAsync(IEnumerable<short> skillIds);

    Task<bool> SkillExistsAsync(string name);

    Task<Skill> AddSkillAsync(Skill skill);

    Task DeleteSkillAsync(short skillId);
}
