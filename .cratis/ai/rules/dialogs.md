---
applyTo: "**/*.tsx"
profile: application
paths:
  - "**/*.tsx"
---
<!-- cratis-ai-managed: rules/dialogs.md -->

# Using Dialogs

The Cratis dialogs handle command execution, validation timing, busy state, focus, dismissal and footer buttons consistently. A vendor dialog, or a hand-rolled modal, bypasses all of this and leads to inconsistent UX — and a second focus trap around a Components dialog breaks keyboard and screen-reader behavior.

## Choose the Correct Dialog Type

- If confirm executes a command, use `CommandDialog` from `@cratis/components/CommandDialog`.
- If no command is executed on confirm, use `Dialog` from `@cratis/components/Dialogs`.
- If you are **asking the user to confirm** or **showing that something is in progress**, do not build a dialog at all — raise the host-rendered one through its hook (below).
- **Never** use a vendor or hand-rolled modal. Dialogs are Components-owned; the optional renderer adapters do not replace them.

## Confirmations and busy indicators are host-rendered — register once, raise by hook

`ConfirmationDialog` and `BusyIndicatorDialog` are **not** instantiated in a slice's JSX. They are
registered once at the app root and raised from anywhere through a hook, so every confirmation and
every busy indicator in the application looks and behaves identically:

```tsx
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';

export const App = () => (
    <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
        <YourApp />
    </DialogComponents>
);
```

```tsx
import { DialogButtons, DialogResult, useConfirmationDialog, useBusyIndicator } from '@cratis/arc.react/dialogs';

const [confirm] = useConfirmationDialog();
const answer = await confirm('Delete this alert?', `"${alert.title}" disappears permanently.`, DialogButtons.YesNo);
if (answer !== DialogResult.Yes) return;

const [showBusy, closeBusy] = useBusyIndicator('Importing', 'This takes a moment.');
showBusy();
try { await doTheSlowThing(); } finally { closeBusy(); }
```

**Rules:**
- Register both in **exactly one** place — the app root. A second registration, or a slice building its own confirm/busy dialog, is how two of them end up looking different.
- Reach them only via `useConfirmationDialog` / `useBusyIndicator`. Never hand-roll a Yes/No `Dialog`, and never use `window.confirm`.
- `showConfirm()` resolves to a `DialogResult` — branch on the enum member, never on button text.
- Always pair `showBusy()` with `closeBusy()` in a `finally`; the busy dialog is deliberately non-dismissible, so a missed close leaves the user stuck.
- **A busy indicator is a modal for work that blocks the user.** For a quick command behind a button, an in-flight/disabled button (eventual-consistency rule 9) is the better control — a modal that flashes for 200 ms is worse than no modal. Use the busy dialog when the user genuinely cannot proceed.
- From a view model, use the injectable `IDialogs` abstraction (`@cratis/arc.react.mvvm/dialogs`) rather than the hooks — see [react.md](./react.md).

## When Using `CommandDialog`

- Pass the command constructor to `command={}`. `CommandDialog` handles instantiation, execution, and confirm/cancel buttons.
- Use command form fields (`InputTextField`, `TextAreaField`, etc. from `@cratis/components/CommandForm`) for user-input values.
- `CommandDialog` automatically disables confirm while the command executes.
- Any value that must be present for the form to be considered valid (i.e. passes `validateRequiredProperties`) must be supplied via `initialValues`, **not** via `onBeforeExecute`.
  - `onBeforeExecute` fires only at execution time — the command is already validated before it runs, so values set there never influence `isValid` and the OK/Submit button will remain permanently disabled.
  - Use `initialValues` for injected context values (e.g. a parent entity id passed as a prop).
  - Use `onBeforeExecute` only for transformations that should not affect form validity (e.g. generated IDs).

```tsx
import { DialogProps, DialogResult } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { RegisterProject } from './Registration';
import { Guid } from '@cratis/fundamentals';

export const AddProject = ({ closeDialog }: DialogProps) => {
    return (
        <CommandDialog<RegisterProject>
            command={RegisterProject}
            title="Add Project"
            okLabel="Add"
            cancelLabel="Cancel"
            onBeforeExecute={(values) => {
                values.projectId = Guid.create();  // generated, not user input
                return values;
            }}>
            <InputTextField<RegisterProject>
                value={instance => instance.name}
                title="Project name"
                placeholder="My Project"
            />
        </CommandDialog>
    );
};
```

To await the result from the parent:

```tsx
const [AddProjectDialog, showAddProjectDialog] = useDialog(AddProject);

const [result] = await showAddProjectDialog();
if (result === DialogResult.Ok) {
    // Dialog confirmed and command executed successfully
}
```

### `onSuccess` vs `onConfirm`

- **`onSuccess(response)`** fires only after the command succeeds and receives the typed command response — use it for `closeDialog(DialogResult.Ok, response)`, refreshing a query, or a toast.
- **`onConfirm()`** receives **no** command result. It is a close gate after successful execution: return `true` to let the wrapper close, `false`/`undefined` to keep it open. **Do not use `onConfirm` as a command-result handler.**

### `onBeforeExecute` is a transformer

It receives the current command values and **must return them** (mutated or not). **Returning `void` does not execute with `undefined`** — the current values are kept and a `console.warn` is logged; still, always return the values. It runs only on submit — never use it to seed *required* values (validation runs against pre-transform state, so a value seeded here never makes the form valid and the submit button stays permanently disabled). Seed required values via `initialValues`; reserve `onBeforeExecute` for transforms that don't affect validity (e.g. a generated id).

### CommandForm fields

Use built-in `CommandForm` fields (from `@cratis/components/CommandForm`) for every user-input value — a raw control (a `Common` `TextInput`, a native `<input>`) inside a command dialog is not bound to the command, so validation never re-runs and the submit button stays **permanently disabled**. Catalog: `InputTextField`, `PasswordField`, `NumberField`, `NumberInputField`, `DropdownField`, `CheckboxField`, `ToggleSwitchField`, `TextAreaField`, `CalendarField`, `RadioButtonField`, `RadioGroupField`, `ChipsField`, `MultiSelectField`, `ColorPickerField`, `SliderField`, `RatingField`. `AutoCommandForm` generates the field list from the command's properties (`string` → `InputTextField`, `number` → `NumberField`, `boolean` → `CheckboxField`, `Date` → `CalendarField`; other types need a registered field-type provider) — use it for plain forms, hand-write fields when the layout or a concept type needs it. Several `RadioButtonField`s bound to one property need the same explicit `name`.

- The `value={c => c.name}` **accessor lambda doubles as the binding and type-checked field selection** — renaming a command property surfaces a compile error at every binding. The binding is inferred from the accessor's *source text*, so a computed accessor (`c => c[descriptor.name]`) cannot be inferred — name it explicitly with `fieldName="…"` (Arc ≥ 22.16.0).
- **`RadioGroupField<T>`** renders a whole group from data (`options`/`optionLabel`/`optionValue`, `layout='horizontal'|'vertical'`); **`RadioButtonField<T>`** is one component per option (each takes a `buttonValue`). Both infer the value type from the accessor — no `as string` casts.
- **`asCommandFormField(Component, opts)`** (`asCommandFormField`, `WrappedFieldProps` from `@cratis/arc.react/commands`) wraps a custom input so it participates in `CommandForm` like a built-in. `WrappedFieldProps<T>` gives `{ value, onChange, invalid, required, errors }` (`errors` is `string[]` → `errors.join(', ')`); options are `{ defaultValue, extractValue: e => ... }`.
- **`useCommandInstance<TCommand>()`** (`@cratis/arc.react/commands`, no argument — reads the enclosing form's context) returns the live reactive instance the form is bound to — **read** it to drive dependent fields (e.g. read `command.country` to choose a `DropdownField`'s options); never mutate (mutations go through field bindings).

### Opening dialogs — `useDialog` / `useDialogContext`

`useDialog<TResponse, TInput>(Component)` returns `[Wrapper, showFn, context]` (the third element is rarely needed): render `<Wrapper />` in JSX and call `showFn(input)` to open it; it resolves to `[DialogResult, TResponse?]` when the dialog closes. For a new dialog, prefer reading input as **plain typed props** (`<Name>Input`) and obtaining `closeDialog` from **`useDialogContext<TRequest, TResponse>()`** (request type first) — rather than declaring a props interface that extends `DialogProps` to thread both input and `closeDialog`. (Existing dialogs that destructure `closeDialog` from `DialogProps` remain valid.) Signal the outcome with `closeDialog(DialogResult.Ok | Cancelled, response?)`.

### Multi-step wizards — `StepperCommandDialog`

For a command split across named steps use `StepperCommandDialog` (`@cratis/components/CommandDialog`) — see the **cratis-components-stepper-command-dialog** skill. Conditional steps written as `{condition && <StepperPanel/>}` are supported: only the steps that actually render are counted, so Next and Submit appear where the user expects them, and the active step is clamped if a late-resolving condition removes a step the user had passed. ⚠️ A `<>…</>` fragment wrapping several panels counts as **one** step — give each step its own `StepperPanel` child. Stepper parts are Cratis-owned (`root`, `list`, `step`, `header`, `number`, `title`, `separator`, `panels`, `panel`; `StepperParts` from `@cratis/components/CommandDialog`). When the wizard belongs inline in a page region rather than in a modal, use the sibling `CommandStepper` (`@cratis/components/CommandStepper`) — same steps, same command execution, no dialog chrome.

## When Using `Dialog`

Use this for dialogs that collect data and return it without executing a command (e.g. confirmation prompts, pure data-entry dialogs). `Dialog` defaults to OK + Cancel buttons. Use `isValid` to control confirm button state, `okLabel`/`cancelLabel` to customize button text.

```tsx
import { useState } from 'react';
import { DialogProps, DialogResult } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { TextInput } from '@cratis/components/Common';

export const AddProject = ({ closeDialog }: DialogProps<{ name: string }>) => {
    const [name, setName] = useState('');
    const isValid = name.trim().length > 0;

    return (
        <Dialog
            title="Add Project"
            width='32rem'
            isValid={isValid}
            onConfirm={() => closeDialog(DialogResult.Ok, { name })}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <TextInput
                value={name}
                onChange={value => setName(value)}
                aria-label="Project name"
            />
        </Dialog>
    );
};
```

## Prefer `DialogButtons` over Custom Button JSX

Use the built-in `DialogButtons` enum instead of rendering manual `<Button>` elements in the `buttons` prop:

```tsx
import { DialogButtons, DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
```

### Available Button Sets

| `buttons` value | Shows |
|---|---|
| `DialogButtons.OkCancel` | Ok + Cancel |
| `DialogButtons.YesNo` | Yes + No |
| `DialogButtons.YesNoCancel` | Yes + No + Cancel |
| `DialogButtons.Ok` | Ok only |
| `null` | No buttons (content-only dialog) |

## Customizing Built-in Buttons

Use `okLabel`/`cancelLabel` to rename the buttons, and `isValid` to disable the confirm button:

```tsx
<Dialog
    title="Import Orders"
    visible={true}
    buttons={DialogButtons.OkCancel}
    okLabel="Upload"
    isValid={!!file && !isUploading}
    onConfirm={handleUpload}
    onCancel={() => closeDialog(DialogResult.Cancelled)}
>
```

## Validation Guard — Keep Dialog Open on Failure

When `onConfirm` needs to keep the dialog open (e.g. a command fails), annotate the handler as `Promise<boolean>` and return `false` to block the close. Return `true` to let the Dialog close itself:

```tsx
const handleConfirm = async (): Promise<boolean> => {
    const result = await myCommand.execute();
    if (!result.isSuccess) return false; // dialog stays open
    return true; // dialog closes
};
```

> **TypeScript note:** Always annotate the function as `Promise<boolean>`. Without it TypeScript infers `Promise<false | void>` which does not satisfy the `ConfirmCallback` type.

## Passing Result Data on Confirm

When the dialog must return data to its caller (e.g. a postal code lookup result), use `onClose` and call `closeDialog` manually, returning `false` to prevent the Dialog from calling it a second time:

```tsx
<Dialog
    title="Confirm Location"
    visible={true}
    buttons={DialogButtons.OkCancel}
    isValid={isValid}
    onClose={(result) => {
        if (result === DialogResult.Ok) {
            closeDialog(DialogResult.Ok, { postalCode, city, latitude, longitude } as MyResult);
            return false; // prevent Dialog from calling closeDialog(Ok) again
        }
        // Cancelled: return undefined so the Dialog calls closeDialog(Cancelled)
    }}
>
```

## Content-only Dialogs (No Action Buttons)

Use `buttons={null}` for dialogs that contain their own internal actions (e.g. a menu + data table) and don't need a confirm/cancel footer:

```tsx
<Dialog
    title="Hubs"
    visible={true}
    width="50vw"
    buttons={null}
    onCancel={() => closeDialog(DialogResult.Cancelled)}
>
    <Menubar model={menuItems} />
    <Listing configurationId={configurationId} />
</Dialog>
```

## Props Reference

| Prop | Type | Notes |
|---|---|---|
| `title` | `string` | Header text |
| `visible` | `boolean` | Controlled open state; defaults to `true` |
| `buttons` | `DialogButtons \| ReactNode \| null` | Prefer `DialogButtons` enum; `null` for no footer |
| `isValid` | `boolean` | Disables the confirm button when `false` |
| `isBusy` | `boolean` | Disables every action **and every dismissal path** while work is in flight |
| `initialFocus` | `DialogInitialFocus` | Where focus lands on open; defaults to the confirmation action — the **cratis-components-accessibility** skill covers the armed-Enter hazard |
| `okLabel` / `cancelLabel` / `yesLabel` / `noLabel` | `string` | Per-dialog label; falls back to the provider's `messages.dialog`, then English |
| `closeAriaLabel` | `string` | Accessible name of the header close button; same fallback chain |
| `onConfirm` | `() => boolean \| void \| Promise<boolean> \| Promise<void>` | Called when Ok is clicked; return `false` to keep dialog open, `true` to close |
| `onCancel` | `() => boolean \| void \| Promise<boolean> \| Promise<void>` | Called when Cancel is clicked |
| `onClose` | `(result: DialogResult) => boolean \| void \| Promise<...>` | Combined handler for both Ok and Cancel |
| `width` | `string` | Dialog width (e.g. `'50vw'`); defaults to `450px` |
| `dismissable` | `boolean` | Header close, Escape and backdrop dismissal (when not busy) |
| `style` / `contentStyle` / `className` | | Modal-root and content-region styling |
| `pt` | `DialogParts` | Per-part attributes: `backdrop`, `positioner`, `root`, `header`, `title`, `close`, `content`, `footer`, `confirm`, `cancel` |

`resizable`, `ptOptions` and `unstyled` are accepted for source compatibility and do nothing. There is no `modal`, `draggable`, `footer` or `onHide` — the dialog is always modal, the footer comes from `buttons`, and closing is reported through `onCancel`/`onClose`. Style the dialog through its parts (`pt`, or `[data-cratis-part='root']` selectors), never through React Aria class names.
