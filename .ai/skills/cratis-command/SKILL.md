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
// Accounts/OpenDebitAccount/OpenDebitAccount.cs — the slice file
namespace MyApp.Accounts.OpenDebitAccount;

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record OpenDebitAccount(AccountId AccountId, AccountName Name, OwnerId OwnerId)
{
    public DebitAccountOpened Handle() =>
        new(Name, OwnerId); // Arc appends the returned event; AccountId is the event source
}

/// <summary>Emitted when a debit account is opened.</summary>
[EventType]  // NO arguments — never [EventType("some-guid")]
public record DebitAccountOpened(AccountName Name, OwnerId OwnerId);
```

**Rules:**
- `[Command]` attribute is **required** — it makes the type discoverable and the analyzer will warn without it
- `Handle()` returns the event (or events) to append — Arc's Chronicle integration automatically appends them; **never inject `IEventLog` to append the primary event**
- `[EventType]` takes **no arguments** — the identifier is generated from the type name
- Name the command as an imperative action — `OpenDebitAccount`, not `OpenDebitAccountCommand`
- All backend artifacts for the slice live in this one file; place it in the slice folder, not an `API/` or `Commands/` folder (see [vertical-slices.md](../../rules/vertical-slices.md))
- Use concept wrappers for every domain value — **identity** concepts derive from `EventSourceId<T>`, **value** concepts from `ConceptAs<T>`; never raw `Guid`/`string`

```csharp
// Accounts/AccountId.cs — identity concept derives from EventSourceId<T>
public record AccountId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static AccountId New() => new(Guid.NewGuid());
    public static implicit operator AccountId(Guid value) => new(value);
}
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

Return `IEnumerable<object>`. Events never carry the event-source id — for events that belong to **different** streams, wrap each in `EventForEventSourceId(id, @event)`:

```csharp
[Command]
public record TransferFunds(AccountId FromId, AccountId ToId, Money Amount)
{
    public IEnumerable<object> Handle() =>
    [
        new EventForEventSourceId(FromId, new FundsWithdrawn(Amount)),
        new EventForEventSourceId(ToId, new FundsDeposited(Amount)),
    ];
}
```

For multiple events on the **same** event source, return the bare event records.

---

## Step 2 — Fetch data the handler needs with `Provide()`

When `Handle()` needs fetched or computed data before it can build the event, add a `Provide()` method. It runs after authorization and validation, resolves its parameters from DI, and binds its return value to `Handle(...)` parameters by type. It may short-circuit with a `ValidationResult.Error(...)` when the data is missing or unusable — **do not throw for that**.

```csharp
[Command]
public record OpenDebitAccount(AccountId AccountId, AccountName Name, OwnerId OwnerId)
{
    public async Task<Result<Owner, ValidationResult>> Provide(IReadModels readModels)
    {
        var owner = await readModels.GetInstanceById<Owner>((EventSourceId)OwnerId);
        return owner is null ? ValidationResult.Error("Owner must exist.") : owner;
    }

    public DebitAccountOpened Handle(Owner owner) => new(Name, owner.Id);
}
```

- Keep IO in `Provide()` and event construction in `Handle()`.
- For **uniqueness**, use `[Unique]` / `IConstraint` (race-safe) — not a read-model pre-check or a throwing service. See the `add-business-rule` skill.
- For a concurrency-sensitive state rule, inject the read model into `Handle()` and return `Result<TEvent, ValidationResult>` (see `add-business-rule`).

Arc automatically wraps the return in `CommandResult` / `CommandResult<T>`. See `references/command-result.md`.

---

## Step 3 — Add validation (optional but recommended)

FluentValidation rules run **on the server** as part of the command pipeline; the proxy generator also extracts them so the same rules run **client-side as pre-flight** validation in `CommandForm`. Put the validator beside the command in the slice file:

```csharp
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
- No registration needed; **omit the validator entirely when there are no rules**
- Arc also creates a `/validate` endpoint automatically; the frontend `command.validate()` calls it without executing the handler
- Single-property intrinsic rules (format, range, required) belong on a `ConceptValidator<T>` for the value concept, so they travel with the value everywhere

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

For modal dialogs, create a dialog component using `DialogProps` and wire it up with `useDialog`:

```tsx
import { DialogProps } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { OpenDebitAccount } from '../api/Accounts/OpenDebitAccount';

// --- Dialog component ---
export const OpenAccountDialog = ({ closeDialog }: DialogProps) => {
    return (
        <CommandDialog<OpenDebitAccount>
            command={OpenDebitAccount}
            title="Open account"
            okLabel="Open"
        >
            <InputTextField<OpenDebitAccount> value={c => c.name} label="Name" />
        </CommandDialog>
    );
};

// --- Parent component ---
import { useDialog, DialogResult } from '@cratis/arc.react/dialogs';
import { OpenAccountDialog } from './OpenAccountDialog';

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

**How it works:**
- `useDialog(OpenAccountDialog)` returns a wrapper component and a `show` function
- `showOpenAccount()` opens the dialog; it returns a Promise that resolves when the dialog closes
- `CommandDialog` executes the command when the user confirms; it closes automatically
- `DialogProps` provides `closeDialog` — needed when you want to pass a response back or handle cancel explicitly

**Edit dialog (pre-populate with existing values):**

```tsx
interface EditAccountDialogProps extends DialogProps {
    accountId: string;
    name: string;
}

export const EditAccountDialog = ({ closeDialog, accountId, name }: EditAccountDialogProps) => {
    return (
        <CommandDialog<UpdateAccount>
            command={UpdateAccount}
            title="Edit account"
            initialValues={{ accountId }}
            currentValues={{ name }}
        >
            <InputTextField<UpdateAccount> value={c => c.name} label="Name" />
        </CommandDialog>
    );
};
```

- `initialValues` — sets the change-tracking baseline (e.g. IDs that must be present for the command but are not user-entered)
- `currentValues` — pre-populates the visible field values

`CommandDialog` calls `onConfirm` only after a successful `command.execute()`, so you don't need to check `isSuccess` yourself. See `references/command-dialog.md` for the full props list.

---

## Reference files

| File | What's in it |
| ---- | ------------ |
| `references/command-result.md` | Full `CommandResult` shape, error handling patterns |
| `references/command-dialog.md` | `CommandDialog` props, CommandForm fields, edit dialogs |
| `references/validation.md` | FluentValidation, Data Annotations, client-side pre-flight |
| `references/proxy-setup.md` | First-time proxy generator setup |
