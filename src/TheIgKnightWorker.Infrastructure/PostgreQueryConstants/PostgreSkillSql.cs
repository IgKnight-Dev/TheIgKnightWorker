using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheIgKnightWorker.Infrastructure.PostgreQueryConstants;

internal class PostgreSkillSql
{
    public const string GetSkills = @"
        SELECT *
        FROM Skills
        ORDER BY Name;";

    public const string GetSkillById = @"
        SELECT *
        FROM Skills
        WHERE Id = @Id;";

    public const string AddSkill = @"
        INSERT INTO Skills
        (Name, CreatedAt)
        VALUES
        (@Name, @CreatedAt)
        RETURNING Id;";

    public const string DeleteSkill = @"
        DELETE FROM Skills
        WHERE Id = @Id;";

    public const string SkillExists = @"
        SELECT COUNT(1)
        FROM Skills
        WHERE LOWER(Name) = LOWER(@Name);";

    public const string GetSkillsByIds = @"
        SELECT *
        FROM Skills
        WHERE Id IN @Ids
        ORDER BY Name;";
}
