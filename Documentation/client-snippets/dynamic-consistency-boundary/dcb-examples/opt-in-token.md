```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record SignUpInitiated(string Email, string TokenHash, DateTimeOffset ExpiresAt);

[EventType]
public record SignUpConfirmed;

// Both events live on the sign-up's own event source. The token is checked against the sign-up
// the caller names, so the token itself needs no place in the boundary.
[FromEvent<SignUpInitiated>]
public record PendingSignUp(Guid Id, string TokenHash, DateTimeOffset ExpiresAt, [Count<SignUpConfirmed>] int Confirmations);

public enum ConfirmOutcome
{
    Confirmed,
    UnknownSignUp,
    AlreadyConfirmed,
    WrongToken,
    Expired,
    ChangedConcurrently,
    Failed
}

public static class ConfirmSignUp
{
    public static async Task<ConfirmOutcome> Handle(IEventStore eventStore, Guid signUpId, string tokenHash, DateTimeOffset now)
    {
        var read = await eventStore.GetDecisionReads().GetDetached<PendingSignUp>(signUpId);
        if (read.Instance is not { } signUp)
        {
            return ConfirmOutcome.UnknownSignUp;
        }

        if (signUp.Confirmations > 0)
        {
            return ConfirmOutcome.AlreadyConfirmed;
        }

        if (signUp.TokenHash != tokenHash)
        {
            return ConfirmOutcome.WrongToken;
        }

        if (signUp.ExpiresAt < now)
        {
            return ConfirmOutcome.Expired;
        }

        // Two requests with the same valid token race: the second append is rejected.
        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(signUpId, new SignUpConfirmed())],
            guardedBy: [read]);

        if (result.GetDecisionConflicts([read]).Any())
        {
            return ConfirmOutcome.ChangedConcurrently;
        }

        return result.IsSuccess ? ConfirmOutcome.Confirmed : ConfirmOutcome.Failed;
    }
}
```
