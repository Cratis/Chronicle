<!-- cratis-ai-managed: skills/cratis-arc-command/references/causation-details.md -->
# Causation details

This reference holds a worked `[NotAudited]` example and how `[NotAudited]` and `ARCCHR0009` behave.

```csharp
[Command]
public record Change<Secret>(
    <UserId> User,
    [property: NotAudited] string Old<Secret>,
    [property: NotAudited] string New<Secret>)
{
    public <SecretChanged> Handle(I<Hasher> hasher) => new(hasher.Hash(New<Secret>));
}
```

`[NotAudited]` applies to a class, struct, property, or parameter. On the type it
withholds every property at once. Marking the concept type instead makes it
travel to every command that takes that value. The command is still named on the
chain either way; only the values are withheld.

`ARCCHR0009` warns when a property's *name* reads like a secret and is unmarked.
It cannot see a secret whose name does not say so, so a clean build means
"nothing obvious was missed", not "no secrets are recorded". When the name only
reads like a secret and the value is safe to record, the framework's own guidance
is to mark it `[NotAudited]` anyway or rename the property — the value is written
either way, so the reading is all a reviewer has to go on.
