<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/dialogs.md -->
# Dialogs reference

The Cratis dialog wrappers own command execution, validation timing, busy
state, and footer buttons. Never import `Dialog` from `primereact/dialog`.

The **components** live in `@cratis/components`; the **hooks, enums, and
context** live in `@cratis/arc.react/dialogs`. `@cratis/components` exports no
dialog hooks at all.

```tsx
import { CommandDialog } from '@cratis/components/CommandDialog';
import { BusyIndicatorDialog, ConfirmationDialog, Dialog } from '@cratis/components/Dialogs';
import { DialogInitialFocus } from '@cratis/components/Dialogs';
import { InputTextField } from '@cratis/components/CommandForm';
import {
    DialogButtons, DialogComponents, DialogResult, useDialog, useDialogContext,
} from '@cratis/arc.react/dialogs';
```

## Choose the dialog

- Confirm runs a command → `CommandDialog`.
- Confirm returns data without a command → `Dialog`.
- Asking the user to confirm, or blocking them while work runs → do not build a
  dialog; raise the host-registered one by hook.
- A command split over named steps → `StepperCommandDialog`, see the
  **cratis-components-stepper-command-dialog** skill.

## Opening a dialog

```tsx
const [AccountDialog, showAccountDialog] = useDialog(CreateAccountDialog);
// render <AccountDialog /> once in the tree
const [result, response] = await showAccountDialog({ accountId });
if (result === DialogResult.Ok) { /* ... */ }
```

`useDialog<TResponse, TInput>(Component)` returns a three-element tuple — the
wrapper component, the show function, and the dialog context. Destructuring the
first two is the normal case. `showDialog(input?)` resolves to
`[DialogResult, TResponse?]` when the dialog closes.

Inside the dialog, prefer plain typed props for input and take `closeDialog`
from `useDialogContext<TResponse>()`:

```tsx
const { request, closeDialog } = useDialogContext<AccountCreated>();
closeDialog(DialogResult.Ok, response);
```

Dialogs that destructure `closeDialog` from a props interface extending
`DialogProps` remain valid.

## The enums

```ts
enum DialogResult { None = 0, Yes = 1, No = 2, Ok = 3, Cancelled = 4 }
enum DialogButtons { Ok = 1, OkCancel = 2, YesNo = 3, YesNoCancel = 4 }
enum DialogInitialFocus { Confirm = 1, Cancel = 2, Content = 3 }
```

Branch on the enum member, never on button text.

## `CommandDialog`

`CommandDialog` combines the command form and the dialog: its props are every
`Dialog` prop except `children`, plus every command-form prop except `children`
and `onBeforeExecute`, plus its own `onBeforeExecute` and `children`. `buttons`
defaults to `DialogButtons.OkCancel`.

The most-used props:

| Prop | Purpose |
| --- | --- |
| `command` | the generated command class |
| `title` | dialog header |
| `okLabel` / `cancelLabel` | button labels (`'Ok'` / `'Cancel'` by default) |
| `initialValues` | synchronous baseline; also the change-tracking baseline |
| `currentValues` | reactive overlay for late-loading values |
| `isValid` | extra validity gate on top of field validation |
| `onBeforeExecute` | transform values just before execute |
| `onSuccess` | receives the typed command response after a successful execute |
| `onValidationFailure` / `onFailed` | failure branches |
| `onConfirm` / `onCancel` / `onClose` | close gates |
| `validateOn` | `'blur' \| 'change' \| 'both'` |
| `validateOnInit` | validate on mount so pre-filled invalid values show immediately |
| `initialFocus` | which control receives focus when the dialog opens |
| `dismissable` | X, Escape, and backdrop dismissal |
| `pt` / `ptOptions` / `unstyled` | PrimeReact pass-through |

`CommandDialog.Column` exists for multi-column layouts:

```tsx
<div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
    <CommandDialog.Column>
        <InputTextField value={(c: UpdateProfile) => c.firstName} title='First name' />
    </CommandDialog.Column>
    <CommandDialog.Column>
        <InputTextField value={(c: UpdateProfile) => c.lastName} title='Last name' />
    </CommandDialog.Column>
</div>
```

### Execution flow

On confirm, `CommandDialog` applies `onBeforeExecute`, sets busy, executes the
command, and on failure calls `onValidationFailure(result.validationResults)`
or `onFailed(result)` and **keeps the dialog open**. On success it calls
`onSuccess(result.response)` and then the close gate.

### `initialValues` versus `onBeforeExecute`

Validation runs against the pre-transform values, so a required value seeded in
`onBeforeExecute` never makes the form valid and the submit button stays
permanently disabled. Seed required values with `initialValues`; reserve
`onBeforeExecute` for transforms that do not affect validity, such as a
generated id.

`onBeforeExecute` is a **transformer** — it receives the current values and must
return them (a `TCommand` or a `Promise<TCommand>`). Returning `undefined`
keeps the current values and logs a console warning rather than executing with
undefined values, but it is still a bug: always return the values.

### `onSuccess` versus `onConfirm`

`onSuccess(response)` fires only after the command succeeds and carries the
typed response — use it to close, refresh, or toast. `onConfirm()` receives no
command result; it is a close gate that must return exactly `true` to let the
wrapper close. Do not use `onConfirm` as a result handler.

## `CommandForm` fields

Every user-entered command value must be bound through a `CommandForm` field
from `@cratis/components/CommandForm` (also published as
`@cratis/components/CommandForm/fields`, the same module). A raw PrimeReact
control bypasses the field wrapper, so validation never re-runs and the submit
button stays disabled.

The full catalog: `InputTextField`, `PasswordField`, `NumberField`,
`TextAreaField`, `CheckboxField`, `ToggleSwitchField`, `DropdownField`,
`MultiSelectField`, `RadioButtonField`, `RadioGroupField`, `CalendarField`,
`ChipsField`, `ColorPickerField`, `SliderField`, `RatingField`.

Shared field props: `value` (the accessor), `title` (the **label**),
`description`, `required`, `icon` (a `React.ReactElement`), `initialValue`,
`noInitialValue`, `populationKey`. `CheckboxField`, `ToggleSwitchField`,
`RadioButtonField`, and `RadioGroupField` additionally take their own `label`.

```tsx
<InputTextField<CreateAccount> value={c => c.name} title='Account name' placeholder='Acme' />
<NumberField<CreateAccount> value={c => c.limit} title='Limit' min={0} />
<DropdownField<CreateAccount> value={c => c.status} title='Status' options={statuses} optionLabel='name' optionValue='id' />
<CheckboxField<CreateAccount> value={c => c.isActive} label='Active' />
<CalendarField<CreateAccount> value={c => c.opensOn} title='Opens on' showIcon />
```

`DropdownField` requires `options`, `optionLabel`, and `optionValue`;
`RadioGroupField` requires all three too and takes `layout='horizontal' | 'vertical'`;
`RadioButtonField` is one component per option and requires `buttonValue`.

The `value` accessor doubles as the binding and as type-checked field
selection — renaming a command property produces a compile error at every
binding.

`AutoCommandForm` renders a field per command property from the generated
property descriptors, with `exclude` for the ones you do not want:

```tsx
<AutoCommandForm command={RegisterInvoice} exclude={['invoiceId']} />
```

## `Dialog` — data without a command

```tsx
export const RenameDialog = () => {
    const { closeDialog } = useDialogContext<{ name: string }>();
    const [name, setName] = useState('');

    return (
        <Dialog
            title='Rename'
            width='32rem'
            isValid={name.trim().length > 0}
            onConfirm={() => closeDialog(DialogResult.Ok, { name })}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <InputText value={name} onChange={event => setName(event.target.value)} autoFocus />
        </Dialog>
    );
};
```

`DialogProps` defaults: `visible` `true`, `buttons` `DialogButtons.OkCancel`,
`initialFocus` `DialogInitialFocus.Confirm`, `width` `'450px'`, `isValid` true
when omitted, `isBusy` `false`, `okLabel` `'Ok'`, `cancelLabel` `'Cancel'`,
`yesLabel` `'Yes'`, `noLabel` `'No'`, `closeAriaLabel` `'Close'`.

Two behaviors worth knowing:

- `resizable` is accepted for compatibility but has **no effect** — the
  PrimeReact 11 headless dialog has no resize.
- Passing a custom node to `buttons` instead of a `DialogButtons` member
  removes the header close control, disables Escape, and means `onClose`,
  `onCancel`, and `onConfirm` are never invoked. Close through
  `useDialogContext().closeDialog(...)`, or set `dismissable` explicitly.

### Keeping the dialog open on failure

`onConfirm` must return exactly `true` to close. Returning `false` keeps it
open. Annotate the handler as `Promise<boolean>` so TypeScript does not infer
`Promise<false | void>`:

```tsx
const handleConfirm = async (): Promise<boolean> => {
    const result = await command.execute();
    return result.isSuccess;
};
```

`onClose(result)` is the combined handler for both outcomes and closes unless it
returns `false`.

### Initial focus

`DialogInitialFocus.Confirm` is the default and *arms* the confirm button — a
browser fires `click` from the `keydown` of Enter, so a held Enter confirms.
For a destructive dialog use `DialogInitialFocus.Cancel`, which focuses the
least destructive action and degrades to `Content` when the button set has
nothing to dismiss with. `DialogInitialFocus.Content` focuses the title so
nothing is armed. There is deliberately no "focus nothing" option — a modal
must move focus into itself.

## Host-registered confirmation and busy indicators

`ConfirmationDialog` takes no props at all; it reads its title, message, and
buttons from the dialog request. `BusyIndicatorDialog` takes the busy-indicator
request. Register both once at the app root through `DialogComponents` and
raise them by hook — see the skill's step 5. From a view model, use the
injectable `IDialogs` abstraction instead of the hooks.
