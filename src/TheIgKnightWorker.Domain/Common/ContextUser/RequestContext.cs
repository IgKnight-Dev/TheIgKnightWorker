using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using TheIgKnightWorker.Domain.Entities;
using TheIgKnightWorker.Domain.UnitOfWork;

namespace TheIgKnightWorker.Domain.Common.ContextUser;

public class RequestContext : IRequestContext
{
    private readonly IMemoryCache _Cache;

    private readonly IUnitOfWork _UnitOfWork;

    private static readonly TimeSpan UserCacheDuration = TimeSpan.FromDays(2);

    public ContextUser User { get; set; } = new ContextUser();

    public RequestContext(IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, IUnitOfWork unitOfWork)
    {
        this._Cache = memoryCache;
        this._UnitOfWork = unitOfWork;

        var user = httpContextAccessor.HttpContext?.User;

        var sub = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                  user?.FindFirst("sub")?.Value;

        if (Guid.TryParse(sub, out var parsedId))
            this.User.UserIdentifier = parsedId;

        this.User.Email = user?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        this.DoThings(user).GetAwaiter().GetResult();
    }

    private async Task DoThings(ClaimsPrincipal user)
    {
        this.User = await this.GetUserDetails(this.User);

        var privilegesClaim = user?.FindFirst("privileges")?.Value;
        var featuresClaim = user?.FindFirst("features")?.Value;

        if (!string.IsNullOrEmpty(privilegesClaim))
        {
            try
            {
                this.User.Privileges = System.Text.Json.JsonSerializer.Deserialize<List<string>>(privilegesClaim) ?? new();
            }
            catch { /* log or ignore */ }
        }

        if (!string.IsNullOrEmpty(featuresClaim))
        {
            try
            {
                this.User.Features = System.Text.Json.JsonSerializer.Deserialize<List<string>>(featuresClaim) ?? new();
            }
            catch { /* log or ignore */ }
        }
    }

    private async Task<ContextUser> GetUserDetails(ContextUser contextUser)
    {
        string cacheKey = $"ContextUser:{contextUser.UserIdentifier}";

        if (_Cache.TryGetValue(cacheKey, out ContextUser cachedData))
        {
            return cachedData;
        }

        var user = await this.GetUserAsync(contextUser.UserIdentifier);

        contextUser.UserName = user?.Username;
        contextUser.Email = user?.Email;

        _Cache.Set(cacheKey, contextUser, UserCacheDuration);

        return contextUser;
    }

    private async Task<User> GetUserAsync(Guid userId)
    {
        return await this._UnitOfWork.ContextUserRepository.GetUserByUserIdAsync(userId);
    }
}