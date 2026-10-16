---
name: cratis-arc-react-page
description: "Build a React page in a Cratis Arc application with Cratis Components — DataPage lists, columns, toolbar menu items, command dialogs, confirmation and busy-indicator dialogs, row selection, details panes, snapshot and observable queries, paging, and MVVM view models. Use when building or changing a page that lists or displays data, wiring Add/Edit/Delete actions, connecting a component to a generated Arc query or command proxy, asking the user to confirm something, or implementing a `.play` screen or form as a page. Not for: a multi-step wizard dialog alone (use `cratis-components-stepper-command-dialog`), a canvas tool palette (use `cratis-components-toolbar`), theming (use `cratis-components-styling`), backend commands and read models (use `cratis-arc-command`, `cratis-chronicle-read-model`), or editing the `.play` model itself (use `cratis-screenplay-event-modeling`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-react-page/SKILL.md -->

# Cratis Arc React pages

Build the page from the generated Arc proxies and the Cratis Components
wrappers. Do not hand-roll a table, a dialog, or a fetch.

- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Read the model's `screen` and `form` with
  [from-screenplay.md](references/from-screenplay.md). Edit code here only for
  infrastructure, clients, adapters, Screenplay code attachments, or gap-fill
  scope (`cratis-screenplay-render-and-gap-fill`); never edit Stage-managed
  output.

## Product sources

This skill follows the `@cratis/components` package manifest (`exports`, peers), `Source/DataPage/*`,
`Source/DataTables/*`, `Source/Dialogs/*`, `Source/CommandDialog/*` and `Source/MIGRATION.md`.
Peers declared by `@cratis/components`: `@cratis/arc` and `@cratis/arc.react` and `react`.

Requires `@cratis/arc` and `@cratis/arc.react` 20.3.1 or later (below 23) and `react` 19 or later, as `@cratis/components` declares them peers.

`@cratis/arc.react.mvvm` ships with `@cratis/arc.react` and is an explicit
application dependency.

## Import from subpaths, never the root barrel

The root `@cratis/components` entry is **setup-only** — `CratisComponentsProvider`
and its configuration types. `import { DataPage } from '@cratis/components'` is a
compile error; the Components 3 root namespaces are gone. Always import from the
subpath:

```tsx
import { DataPage, MenuItem, Column } from '@cratis/components/DataPage';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { Dialog } from '@cratis/components/Dialogs';
import { DialogProps, DialogResult, useDialog } from '@cratis/arc.react/dialogs';
```

## Initial focus

`DialogInitialFocus.Confirm` is the default and arms the confirm button, so a destructive dialog uses `DialogInitialFocus.Cancel`. Read [dialogs.md](references/dialogs.md) for the three options.

## Step 1 — Prerequisites

- The backend query and command must already exist, and a Debug build must have
  regenerated the TypeScript proxies. Generated proxies carry a
  `**DO NOT EDIT** - This file is an automatically generated file.` header —
  fix the C# source and rebuild instead of editing one.
- The app must mount `CratisComponentsProvider` (from the package root,
  `@cratis/components`) above every Cratis component — it carries the locale,
  the Components-owned labels. Enable `toaster` (or mount one `<Toaster />`)
  for notifications; without it even `toastCommandResult` displays nothing.
  Mount exactly one toaster, not both. The `cratis` template mounts
  `<CratisComponentsProvider toaster>` **inside** `<Arc>`
  (`Templates/Cratis/.frontend/main.tsx`; Requires Templates 1.7.3 or later, earlier ones mount it outside `<Arc>` without a toaster),
  so check its entry point rather than adding a second toaster.
- The app must import the stylesheets explicitly; components do not import
  their own CSS:

  In `index.css`, imported by the entry with `import './index.css'`:

  ```css
  @import '@cratis/components/tokens';
  @import '@cratis/components/styles';
  @import '@cratis/components/theme'; /* optional baseline */
  ```

  CSS subpath imports from TypeScript need ambient declarations on TypeScript 6;
  use CSS `@import` to avoid TS2882. For a smaller bundle use `styles/base` plus
  the needed area sheets (`DataPage/styles`, etc.); area order does not matter.
  Import `reflect-metadata` at the entry point; it remains required. `tsyringe`
  is optional for Components but installed by Arc React.

  Components has no vendor UI dependency — do not install PrimeReact or an icon
  font for it. Menu icons are React component types (`react-icons` works).

  See the **cratis-components-styling** skill for the full theming contract.

## Step 2 — Trace every field to its origin

Before writing JSX, list every value the page shows or captures, and where it
comes from. A value with no origin gets faked on the client or invented as a
proxy property, so find the gap before the page exists.

| Origin | Written as | Bound through |
| --- | --- | --- |
| Query result | `<Query>.<property>` | `DataPage` `query`, `Column field`, `detailsComponent` |
| Command input | `<Command>.<property>` | a `CommandForm` field `value` accessor |
| Route parameter | `route:<name>` | the view model's route parameters (Step 9) |
| Display-only calculation | `derived:<expression>` | a view-model getter, with a spec |

- The caller's identity is never a command input. Take it on the server, not
  from a form field.
- A value with no origin is a gap in the backend or the model, not in the page.
  Do not compute it from other queries on the client and do not add a property to
  a generated proxy. Stop building that part of the page and add the property to
  the read model or command first (`cratis-arc-command`,
  `cratis-chronicle-read-model`) or, where one exists, to the `.play` model.
- A component that shows one part of a page lists only that part's fields.
- When the page implements a `.play` screen or form, the model is the source of
  the list: follow [from-screenplay.md](references/from-screenplay.md).

## Step 3 — The DataPage shell

`DataPage` owns the query subscription, paging, selection, action menubar, and
the optional details split. Do not pre-fetch rows and pass an array.

Required props: `title`, `query`, `emptyMessage`, `children`. Columns and
toolbar actions are compositional children.

```tsx
import { Column, DataPage } from '@cratis/components/DataPage';
import { AllAccounts } from './AllAccounts';

export const AccountsPage = () => (
    <DataPage title='Accounts' query={AllAccounts} emptyMessage='No accounts yet.' dataKey='id'>
        <DataPage.Columns>
            <Column field='name' header='Name' sortable />
            <Column field='balance' header='Balance' />
        </DataPage.Columns>
    </DataPage>
);
```

`DataPage.Columns`, `DataPage.MenuItems` and `DataPage.MenuItem` are the
compound members; `MenuItem` and `Column` are also **named exports** of the
subpath (there is no `DataPage.Column`). Pass `dataKey` whenever the read model has an
identity.

Read [data-page.md](references/data-page.md) when configuring DataPage props; it lists every prop, and
[data-tables.md](references/data-tables.md) when you need a table without the
page chrome.

## Step 4 — Toolbar actions

Menu items go in `<DataPage.MenuItems>`. `MenuItem` takes `command`, not
`onClick`, and its `icon` is a **React component type**, not an icon class
string. `disableOnUnselected` greys the item out until a row is selected.

```tsx
import { FaPencil, FaPlus } from 'react-icons/fa6';
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import { useDialog } from '@cratis/arc.react/dialogs';
import { CreateAccountDialog } from './CreateAccountDialog';

const [CreateAccountWrapper, showCreateAccount] = useDialog(CreateAccountDialog);

<DataPage title='Accounts' query={AllAccounts} emptyMessage='No accounts yet.'>
    <DataPage.MenuItems>
        <MenuItem label='Add account' icon={FaPlus} command={() => showCreateAccount()} />
        <MenuItem label='Edit account' icon={FaPencil} command={() => showEditAccount()} disableOnUnselected />
    </DataPage.MenuItems>
    <DataPage.Columns>
        <Column field='name' header='Name' />
    </DataPage.Columns>
</DataPage>
<CreateAccountWrapper />
```

Render the wrapper returned by `useDialog` once in the tree. Read
[dialogs.md](references/dialogs.md) when implementing dialog opening, closing, input, response, validation, or focus behavior; it gives the full dialog contract.

## Step 5 — Command dialogs

A dialog that runs a command is its own component built on `CommandDialog`.
Bind each input with a `CommandForm` field whose `value` accessor selects the
command property; the label prop is `title`.

```tsx
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateAccount } from './CreateAccount';

export const CreateAccountDialog = () => {
    const { closeDialog } = useDialogContext();

    return (
        <CommandDialog<CreateAccount>
            command={CreateAccount}
            title='Create account'
            okLabel='Create'
            onSuccess={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <InputTextField<CreateAccount> value={c => c.name} title='Account name' />
        </CommandDialog>
    );
};
```

Never put a raw control (a `Common` `TextInput`, a native `<input>`) inside a
command dialog for a command value — it is not bound to the command, so
validation never re-runs and the submit button stays disabled. Seed values that must be present for validity with
`initialValues`, not `onBeforeExecute`.

## Step 6 — Confirming, and showing that work is in progress

Do not build a confirmation or busy dialog into a page: register `ConfirmationDialog` and `BusyIndicatorDialog` once at the app root through `DialogComponents` and raise them by hook (`useConfirmationDialog`, `useBusyIndicator`). A busy indicator is modal and non-dismissible, so reserve it for work the user cannot proceed past, and pair `showBusy()` with a close in a `finally`. Read [dialogs.md](references/dialogs.md) for the registration and hook examples.

## Step 7 — Row selection and a details pane

`selection` and `onSelectionChange` are controlled (`value` is `null` when cleared), and `detailsComponent` receives `{ item, onRefresh? }` for the selected row. Read [data-page.md](references/data-page.md) for the controlled-selection example.

## Step 8 — Snapshot or observable query

The **same `query` prop** takes a snapshot query or an observable query. There
is no separate `observableQuery` prop: `DataPage` inspects the query prototype
and picks the snapshot or observable table itself. Pass an observable query and
the page stays live. `onRefresh` only forwards a caller callback to the details
component; it never refetches the table. DataPage has no snapshot refresh handle.
Use an observable query when commands must update the list.

Read [queries-and-commands.md](references/queries-and-commands.md) when using generated proxy hooks, implementing paging, or handling command results; it covers the
generated proxy hooks, their exact return tuples, paging, and how to read a
command result.

## Step 9 — MVVM for pages with real logic

Extract a view model as soon as the component has three or more `useState`
calls, a state-synchronizing `useEffect`, or derived values. Keep the component
declarative and read `viewModel.x` **inside** JSX so MobX tracks it.

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';
import { injectable } from 'tsyringe';

@injectable()
export class AccountsViewModel {
    selected: AccountSummary | null = null;
    select(account: AccountSummary | null) { this.selected = account; }
}

export const AccountsPage = withViewModel(AccountsViewModel, ({ viewModel }) => (
    <DataPage title='Accounts' query={AllAccounts} emptyMessage='No accounts yet.' dataKey='id'
        selection={viewModel.selected}
        onSelectionChange={event => viewModel.select(event.value)}>
        <DataPage.Columns>
            <Column field='name' header='Name' />
        </DataPage.Columns>
    </DataPage>
));
```

`withViewModel` applies `makeAutoObservable` itself — do not call it in the
constructor. Read [mvvm.md](references/mvvm.md) when implementing view-model injection, route parameters,
props handling, or teardown.

## Quick decision guide

| Need | Use |
| --- | --- |
| Read-only list page | `DataPage` with a snapshot query |
| Live-updating list page | `DataPage` with an observable query on the same `query` prop |
| Add or create action | `<DataPage.MenuItems>` + `MenuItem` + `CommandDialog` + `useDialog` |
| Edit the selected row | `selection` / `onSelectionChange` + `CommandDialog` + `initialValues` / `currentValues` |
| Detail for the selected row | `detailsComponent` |
| Table without page chrome | `DataTableForQuery` / `DataTableForObservableQuery` |
| Ask the user to confirm | `useConfirmationDialog` — never `window.confirm`, never a hand-rolled Yes/No dialog |
| Multi-step wizard dialog | the **cratis-components-stepper-command-dialog** skill |
| Canvas tool palette | the **cratis-components-toolbar** skill |
| Complex page state | `withViewModel` |
| A `.play` `screen` or `form` is the spec | [from-screenplay.md](references/from-screenplay.md) |

## Verify

- Every Cratis Components import uses a subpath, not the root barrel.
- `CratisComponentsProvider` wraps the tree, one toaster is mounted for
  notifications, and the CSS entries are imported once via the app's CSS file.
- `DataPage` receives `title`, `query`, `emptyMessage`, and children, and
  `dataKey` when the read model has an identity.
- Columns sit in `<DataPage.Columns>`; menu items sit in
  `<DataPage.MenuItems>` and use `command`, not `onClick`.
- No component pre-fetches rows and passes them to `DataPage` as an array.
- Every command value in a dialog is bound through a `CommandForm` field.
- Values required for validity come from `initialValues`, not
  `onBeforeExecute`.
- Confirmations and busy indicators are raised by hook, registered once at the
  app root.
- Every `showBusy()` has a matching close in a `finally`.
- Every displayed or captured value has an origin from the Step 2 table; none
  is computed from another query on the client.
- Each page's states are designed: empty (`emptyMessage`), loading, a command
  that fails (read through the granular `CommandResult` flags in
  [queries-and-commands.md](references/queries-and-commands.md)), denied (a
  persona that can reach the page but whose policy refuses the action), and the
  state after success.
- When a `.play` model describes the page, the page matches it element by
  element and adds no data or action the model lacks
  ([from-screenplay.md](references/from-screenplay.md)).
- No generated proxy file was edited.
- Lint, the frontend test gate, and the TypeScript build all pass.

## Route near misses

- A multi-step wizard dialog: `cratis-components-stepper-command-dialog`.
- A canvas tool palette: `cratis-components-toolbar`.
- Colors, tokens and theming: `cratis-components-styling`.
- The backend command or read model behind the page: `cratis-arc-command`,
  `cratis-chronicle-read-model`.
- Specifying page behavior with specs: `cratis-application-react-specifications`.

## Lineage

Read [references/provenance.md](references/provenance.md) when checking source attribution or adaptation permissions.
