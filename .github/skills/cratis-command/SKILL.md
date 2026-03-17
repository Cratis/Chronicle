---
name: cratis-command
description: Step-by-step guidance for creating a Cratis Arc command — [Command] record, Handle() method, CommandValidator, proxy generation, and React .use() hook with CommandDialog. Use when adding or creating a command, wiring up a form or button to the backend, working with IEventLog, CommandResult, CommandValidator, CommandDialog, or [Command] attribute.
---

# Creating a Cratis Command

A command represents a user action that changes state. In Cratis Arc the path is:

```
[Command] record + Handle()  →  validator  →  dotnet build  →  TypeScript proxy  →  React .use()
```

The command record **owns its own handler** — no separate controller class required. Follow the steps in order. Jump to the reference files for deeper detail on any step.

---

## Step 1 — Define the C# command record

A command is a **record decorated with `[Command]`** that contains its own `Handle()` method. No separate controller is needed.

```csharp
// API/Accounts/OpenDebitAccount.cs
namespace MyApp.API.Accounts;

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record OpenDebitAccount(AccountId AccountId, string Name, OwnerId OwnerId)
{
    public DebitAccountOpened Handle() =>
        new(Name, OwnerId); // Arc appends the returned event; AccountId is the event source
}

[EventType]  // NO arguments — never [EventType("some-guid")]
public record DebitAccountOpened(string Name, OwnerId OwnerId);
```

**Rules:**
- `[Command]` attribute is **required** — it makes the type discoverable and the analyzer will warn without it
- `Handle()` returns the event (or events) to append — Arc's Chronicle integration automatically appends them
- `[EventType]` takes **no arguments** — the identifier is generated from the type name
- Name the command as an imperative action — `OpenDebitAccount`, not `OpenDebitAccountCommand`
- Use `ConceptAs<T>` wrappers for all identity types — never raw `Guid` or `string`

```csharp
// Concepts/AccountId.cs
using Cratis.Concepts;

public record AccountId(Guid Value) : ConceptAs<Guid>(Value);
```

### Generating a new ID and returning it

```csharp
[Command]
public record RegisterEmployee(string FirstName, string LastName, string Department)
{
    // Return (eventSourceId, event) — Arc uses the first element as the event source ID
    // and sends it back to the client as CommandResult<Guid>.response
    public (EmployeeId, EmployeeRegistered) Handle()
    {
        var employeeId = new EmployeeId(Guid.NewGuid());
        return (employeeId, new(FirstName, LastName, Department));
    }
}
```

### Appending multiple events

```csharp
[Command]
public record TransferFunds(AccountId FromId, AccountId ToId, decimal Amount)
{
    public IEnumerable<object> Handle() =>
    [
        new FundsWithdrawn(FromId, Amount),
        new FundsDeposited(ToId, Amount),
    ];
}
```

---

## Step 2 — Inject dependencies into Handle (when needed)

Handle() parameters are injected by DI — use this for constraints or external services:

```csharp
[Command]
public record OpenDebitAccount(AccountId AccountId, string Name, OwnerId OwnerId)
{
    public async Task<DebitAccountOpened> Handle(
        IUniqueAccountConstraint constraint)
    {
        await constraint.Validate(Name);
        return new(Name, OwnerId);
    }
}
```

Arc automatically wraps the return in `CommandResult` / `CommandResult<T>`. See `references/command-result.md`.

---

## Step 3 — Add validation (optional but recommended)

FluentValidation rules are extracted by the proxy generator and run **client-side** before the request is sent:

```csharp
// API/Accounts/OpenDebitAccountValidator.cs
public class OpenDebitAccountValidator : CommandValidator<OpenDebitAccount>
{
    public OpenDebitAccountValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(100);
        RuleFor(c => c.OwnerId)
            .NotEmpty().WithMessage("Owner is required");
    }
}
```

- Extends `CommandValidator<T>` (not `AbstractValidator<T>`) — this makes it discoverable automatically
- No registration needed
- Arc also creates a `/validate` endpoint automatically; the frontend `command.validate()` calls it without executing the handler

For simple cases, Data Annotations on the record work too:

```csharp
public record OpenDebitAccount(
    [Required] Guid AccountId,
    [Required, MaxLength(100)] string Name,
    [Required] Guid OwnerId);
```

---

## Step 4 — Generate the TypeScript proxy

```bash
dotnet build
```

The `Cratis.Arc.ProxyGenerator.Build` MSBuild package runs during the build and writes TypeScript files to the path configured in your `.csproj`:

```xml
<PropertyGroup>
  <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../Web/src/api</CratisProxiesOutputPath>
</PropertyGroup>
```

This produces `Web/src/api/Accounts/OpenDebitAccount.ts`. For first-time setup see `references/proxy-setup.md`.

---

## Step 5 — Use the command in React

### Inline form (full control)

```tsx
import { OpenDebitAccount } from '../api/Accounts/OpenDebitAccount';

export const OpenAccountForm = () => {
    const [command, setValues] = OpenDebitAccount.use();
    const [error, setError] = useState('');

    const handleSubmit = async () => {
        const result = await command.execute();
        if (result.isSuccess) {
            onSuccess(result.response); // result.response is Guid if you returned one
        } else if (!result.isValid) {
            setError(result.validationResults[0]?.message ?? 'Validation failed');
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <input
                value={command.name}
                onChange={e => (command.name = e.target.value)}
                placeholder="Account name"
            />
            {error && <p className="error">{error}</p>}
            <button disabled={!command.hasChanges}>Open account</button>
        </form>
    );
};
```

**Key properties on the command instance:**

| Property / method | What it does |
| ----------------- | ------------ |
| `command.propName` | Get/set the property value |
| `command.hasChanges` | `true` when any value differs from the initial |
| `command.execute()` | Send the POST, returns `CommandResult` |
| `command.validate()` | Call the validate endpoint (no side effects) |
| `setValues(obj)` | Set multiple properties at once (e.g. from a query result) |

### With initial values (edit scenario)

```tsx
const [command] = UpdateAccount.use({
    accountId: account.id,
    name: account.name,
});
```

### Using `CommandDialog` (quickest path for modal forms)

See Step 6 and `references/command-dialog.md`.

---

## Step 6 — Wrap in a CommandDialog (optional)

For modal dialogs, this is the quickest path:

```tsx
import { useDialog, useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { OpenDebitAccount } from '../api/Accounts/OpenDebitAccount';

// --- Dialog component ---
const OpenAccountDialog = () => {
    const { closeDialog } = useDialogContext();
    return (
        <CommandDialog
            command={OpenDebitAccount}
            title="Open account"
            okLabel="Open"
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputTextField<OpenDebitAccount> value={c => c.name} label="Name" />
        </CommandDialog>
    );
};

// --- Parent component ---
export const AccountsPage = () => {
    const [OpenAccountDialogWrapper, showOpenAccount] = useDialog(OpenAccountDialog);

    const handleOpen = async () => {
        const [result] = await showOpenAccount();
        if (result === DialogResult.Ok) {
            // command already executed inside the dialog — refresh your data here
        }
    };

    return (
        <>
            <button onClick={handleOpen}>Open account</button>
            <OpenAccountDialogWrapper />
        </>
    );
};
```

`CommandDialog` calls `onConfirm` only after a successful `command.execute()`, so you don't need to check `isSuccess` yourself. See `references/command-dialog.md` for the full props list and edit-dialog patterns.

---

## Reference files

| File | What's in it |
| ---- | ------------ |
| `references/command-result.md` | Full `CommandResult` shape, error handling patterns |
| `references/command-dialog.md` | `CommandDialog` props, CommandForm fields, edit dialogs |
| `references/validation.md` | FluentValidation, Data Annotations, client-side pre-flight |
| `references/proxy-setup.md` | First-time proxy generator setup |
