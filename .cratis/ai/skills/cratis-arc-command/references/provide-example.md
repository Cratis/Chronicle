<!-- cratis-ai-managed: skills/cratis-arc-command/references/provide-example.md -->
# Provide example

Validators own ordinary refusals, including read-model existence. For this
command, a `CommandValidator<OpenAccount>` constructor takes
`[LookUpBy(nameof(OpenAccount.OwnerId))] Owner owner`: non-nullable means the owner
must exist, looked up by `OwnerId` rather than the account's root id. For a custom
message, use a nullable model and a validator rule; see
[the validator example](../../cratis-arc-command-validation/references/read-model-resolution.md).
Do not duplicate that check in `Provide()`.

This illustrative excerpt assumes application types `AccountId` (an event-source
id), `OwnerId`, `Owner`, `CreditHistory`, `AccountOpened(CreditHistory history)`, and
`ICreditHistory.GetFor(OwnerId)` returning `Task<CreditHistory?>`. `Provide()`
acquires the history needed by `Handle()`; its only error means that the external
lookup yielded no data, not that an ordinary validation rule failed.

```csharp
/// <summary>Opens an account using acquired credit history.</summary>
/// <param name="Id">The account's event-source id.</param>
/// <param name="OwnerId">The owner to validate and look up.</param>
[Command]
public record OpenAccount(AccountId Id, OwnerId OwnerId)
{
    /// <summary>Acquires the credit history used by the decision.</summary>
    /// <param name="histories">The external history lookup.</param>
    /// <returns>History, or an error when the lookup yields no data.</returns>
    public async Task<Result<CreditHistory, ValidationResult>> Provide(ICreditHistory histories)
    {
        var history = await histories.GetFor(OwnerId);
        return history is null
            ? ValidationResult.Error("No credit history available.")
            : history;
    }

    /// <summary>Builds the account-opened fact from the acquired history.</summary>
    /// <param name="history">The history supplied by Provide.</param>
    /// <returns>The account-opened fact.</returns>
    public AccountOpened Handle(CreditHistory history) => new(history);
}
```

Only acquisition yielding nothing usable short-circuits here. Keep validation in
the validator, IO in `Provide()` and the event decision in `Handle()`.
