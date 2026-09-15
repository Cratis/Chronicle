---
name: cratis-arc-react-page
description: Build a React page in a Cratis Arc application with Cratis Components — DataPage lists, columns, toolbar menu items, command dialogs, confirmation and busy-indicator dialogs, row selection, details panes, snapshot and observable queries, paging, and MVVM view models. Use when building or changing a page that lists or displays data, wiring Add/Edit/Delete actions, connecting a component to a generated Arc query or command proxy, or asking the user to confirm something. Do not use for a multi-step wizard dialog alone, for a canvas tool palette, or for backend command and read-model work.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-react-page/SKILL.md -->

# Cratis Arc React pages

Build the page from the generated Arc proxies and the Cratis Components
wrappers. Do not hand-roll a table, a dialog, or a fetch.

## Verified product sources

This skill is verified against these exact package contracts:

| Package | Version | Verified from |
| --- | --- | --- |
| `@cratis/components` | `3.0.0` | its package manifest and component sources |
| `@cratis/arc` | `>=20.3.1 <23` | peer range declared by `@cratis/components@3.0.0` |
| `@cratis/arc.react` | `>=20.3.1 <23` | peer range declared by `@cratis/components@3.0.0` |
| `primereact` | `^11.0.0` | peer of `@cratis/components@3.0.0` |
| `primeicons` | `^8.0.0` | peer of `@cratis/components@3.0.0` |
| `react` | `^19.0.0` | peer of `@cratis/components@3.0.0` |

`@cratis/arc.react.mvvm` ships with `@cratis/arc.react` and is an explicit
application dependency. Reverify before claiming another version.

## Import from subpaths, never the root barrel

The root `@cratis/components` entry exports **namespaces**, not components —
`import { DataPage } from '@cratis/components'` yields a namespace object whose
component is `DataPage.DataPage`. Always import from the subpath:

```tsx
import { DataPage, MenuItem, Column } from '@cratis/components/DataPage';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { Dialog } from '@cratis/components/Dialogs';
import { DialogProps, DialogResult, useDialog } from '@cratis/arc.react/dialogs';
```

## Step 1 — Prerequisites

- The backend query and command must already exist, and a Debug build must have
  regenerated the TypeScript proxies. Generated proxies carry a
  `**DO NOT EDIT** - This file is an automatically generated file.` header —
  fix the C# source and rebuild instead of editing one.
- The app must mount `CratisComponentsProvider` (from
  `@cratis/components/Common`) above every Cratis component. PrimeReact 11
  resolves its configuration from a provider, so components fail without one.
- The app must import the stylesheets explicitly; components no longer import
  their own CSS:

  ```ts
  import '@cratis/components/tokens';   // the --cratis-* token layer
  import '@cratis/components/styles';   // every component stylesheet
  import '@cratis/components/theme';    // optional license-free baseline look
  ```

  See the **cratis-components-styling** skill for the full theming contract.

## Step 2 — The DataPage shell

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

Only `DataPage.Columns` and `DataPage.MenuItems` exist as compound members.
`MenuItem` and `Column` are **named exports** — there is no `DataPage.MenuItem`
and no `DataPage.Column`. Pass `dataKey` whenever the read model has an
identity.

See [data-page.md](references/data-page.md) for every prop, and
[data-tables.md](references/data-tables.md) when you need a table without the
page chrome.

## Step 3 — Toolbar actions

Menu items go in `<DataPage.MenuItems>`. `MenuItem` takes `command`, not
`onClick`, and its `icon` is a **React component type**, not an icon class
string. `disableOnUnselected` greys the item out until a row is selected.

```tsx
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import { useDialog } from '@cratis/arc.react/dialogs';
import { CreateAccountDialog } from './CreateAccountDialog';

const [CreateAccountWrapper, showCreateAccount] = useDialog(CreateAccountDialog);

<DataPage title='Accounts' query={AllAccounts} emptyMessage='No accounts yet.'>
    <DataPage.MenuItems>
        <MenuItem label='Add account' icon={() => <i className='pi pi-plus' />} command={() => showCreateAccount()} />
        <MenuItem label='Edit account' icon={() => <i className='pi pi-pencil' />} command={() => showEditAccount()} disableOnUnselected />
    </DataPage.MenuItems>
    <DataPage.Columns>
        <Column field='name' header='Name' />
    </DataPage.Columns>
</DataPage>
<CreateAccountWrapper />
```

Render the wrapper returned by `useDialog` once in the tree. See
[dialogs.md](references/dialogs.md) for the full dialog contract.

## Step 4 — Command dialogs

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

Never put a raw PrimeReact control inside a command dialog for a command value —
it bypasses the field wrapper, so validation never re-runs and the submit button
stays disabled. Seed values that must be present for validity with
`initialValues`, not `onBeforeExecute`.

## Step 5 — Confirming, and showing that work is in progress

Do not build a confirmation or busy dialog into a page. `ConfirmationDialog` and
`BusyIndicatorDialog` are registered once at the app root through
`DialogComponents` and raised by hook, which is what keeps every one of them
identical:

```tsx
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';

<DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
    <YourApp />
</DialogComponents>
```

```tsx
import { DialogButtons, DialogResult, useBusyIndicator, useConfirmationDialog } from '@cratis/arc.react/dialogs';

const [confirm] = useConfirmationDialog();
if (await confirm('Delete this account?', `"${account.name}" disappears permanently.`, DialogButtons.YesNo) !== DialogResult.Yes) return;
```

A busy indicator is modal and deliberately non-dismissible, so reserve it for
work the user genuinely cannot proceed past — a multi-step import, a migration.
For an ordinary command behind a button, the in-flight disabled button is the
right control; a modal that flashes for 200 ms tells the user nothing. When you
do use one, pair it with the close call in a `finally`:

```tsx
const [showBusy, closeBusy] = useBusyIndicator('Importing', 'This takes a moment.');
showBusy();
try { await importEverything(); } finally { closeBusy(); }
```

## Step 6 — Row selection and a details pane

`selection` and `onSelectionChange` are controlled. The change event carries
`value`, which is `null` when the selection is cleared.

```tsx
import type { DataTableSelectionChangeEvent } from '@cratis/components/DataTables';

const [selected, setSelected] = useState<AccountSummary | null>(null);

<DataPage
    title='Accounts'
    query={AllAccounts}
    emptyMessage='No accounts yet.'
    dataKey='id'
    selection={selected}
    onSelectionChange={(event: DataTableSelectionChangeEvent<AccountSummary>) => setSelected(event.value)}
    detailsComponent={AccountDetails}>
    <DataPage.Columns>
        <Column field='name' header='Name' />
    </DataPage.Columns>
</DataPage>
```

`detailsComponent` receives `IDetailsComponentProps<T>` — `{ item, onRefresh? }`
— and renders beside the table for the selected row.

## Step 7 — Snapshot or observable query

The **same `query` prop** takes a snapshot query or an observable query. There
is no separate `observableQuery` prop: `DataPage` inspects the query prototype
and picks the snapshot or observable table itself. Pass an observable query and
the page stays live; pass a snapshot query and call `onRefresh` after a command
succeeds.

Read [queries-and-commands.md](references/queries-and-commands.md) for the
generated proxy hooks, their exact return tuples, paging, and how to read a
command result.

## Step 8 — MVVM for pages with real logic

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
constructor. See [mvvm.md](references/mvvm.md) for injection, route parameters,
props handling, and teardown.

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

## Verify

- Every Cratis Components import uses a subpath, not the root barrel.
- `CratisComponentsProvider` wraps the tree and the three stylesheets are
  imported once at the app entry point.
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
- No generated proxy file was edited.
- Lint, the frontend test gate, and the TypeScript build all pass.
