namespace TheIgKnightWorker.Infrastructure.DbModels;

public class DbSkill
{
    public short Id { get; set; }

    public string Name { get; set; } = default!;
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public short Category { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
