namespace TheIgKnightWorker.Domain.Common.ContextUser;

public class ContextUser
{
    public Guid UserIdentifier { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public List<string> Features { get; set; }
    public List<string> Privileges { get; set; }
}
