```csharp title="Initial values"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

public enum InitialValuesUserStatus
{
    Inactive,
    Active
}

[EventType]
public record InitialValuesUserCreated(string Name, string Email);

public record InitialValuesUserProfile(
    string Name,
    string Email,
    InitialValuesUserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLogin,
    int LoginCount,
    bool IsVerified);

public class InitialValuesUserProfileProjection : IProjectionFor<InitialValuesUserProfile>
{
    public void Define(IProjectionBuilderFor<InitialValuesUserProfile> builder) => builder
        .WithInitialValues(() => new InitialValuesUserProfile(
            Name: "Unknown user",
            Email: string.Empty,
            Status: InitialValuesUserStatus.Inactive,
            CreatedAt: DateTimeOffset.UnixEpoch,
            LastLogin: null,
            LoginCount: 0,
            IsVerified: false))
        .From<InitialValuesUserCreated>(_ => _
            .Set(m => m.Status).ToValue(InitialValuesUserStatus.Active)
            .Set(m => m.CreatedAt).ToEventContextProperty(c => c.Occurred));
}
```
