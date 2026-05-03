# MVVM — Reference

The Arc MVVM pattern keeps page logic in plain TypeScript classes (view models) and keeps components purely declarative.

## When to use MVVM

- Page has complex coordinated state (selected item, filters, multiple dialogs)
- Logic needs unit-testing independent of React
- You want to share state across child components via injection

For simple pages, MVVM is optional — use regular hooks directly in the component instead.

## Setup

Install packages if not already present:

```
npm install @cratis/arc.react.mvvm tsyringe reflect-metadata
```

Ensure `tsconfig.json` enables decorators:

```json
{
    "compilerOptions": {
        "experimentalDecorators": true,
        "emitDecoratorMetadata": true
    }
}
```

Import `reflect-metadata` once, at the entry point of your app:

```tsx
import 'reflect-metadata';
```

## View model class

```ts
import { injectable } from 'tsyringe';
import { makeAutoObservable } from 'mobx';

@injectable()
export class AccountsViewModel {
    selectedAccount?: AccountSummary = undefined;

    constructor() {
        makeAutoObservable(this);
    }

    selectAccount(account: AccountSummary) {
        this.selectedAccount = account;
    }
}
```

- `@injectable()` — registers the class with tsyringe for DI
- `makeAutoObservable(this)` — makes all fields reactive (MobX)

## withViewModel

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';

export const AccountsPage = withViewModel(AccountsViewModel, ({ viewModel }) => {
    return (
        <DataPage
            query={AllAccounts}
            columns={...}
            onRowSelected={(row) => viewModel.selectAccount(row)}
            detailPanel={() => viewModel.selectedAccount
                ? <AccountDetail account={viewModel.selectedAccount} />
                : null
            }
        />
    );
});
```

The view model instance is created once per mount and disposed on unmount. It is the same instance for the whole component tree under `withViewModel`.

## IHandleProps — reactive props

When a child component needs to receive a prop and react to its changes, implement `IHandleProps<T>`:

```ts
import { IHandleProps } from '@cratis/arc.react.mvvm';

interface DetailProps {
    account: AccountSummary;
}

@injectable()
export class AccountDetailViewModel implements IHandleProps<DetailProps> {
    account!: AccountSummary;

    propsChanged(props: DetailProps): void {
        this.account = props.account;
    }
}
```

`propsChanged` is called whenever the parent passes new props, allowing the view model to react.

## Dependency injection in view models

Use tsyringe constructor injection. Cratis registers common singletons (e.g., `IEventStore`, query/command types):

```ts
@injectable()
export class AccountsViewModel {
    constructor(
        private readonly _eventLog: IEventLog,
    ) {
        makeAutoObservable(this);
    }
}
```

## MVVM context

Wrap the app (or route root) in `<MVVM>` to enable the DI container:

```tsx
import { MVVM } from '@cratis/arc.react.mvvm';

<MVVM>
    <App />
</MVVM>
```

If you are using `<Arc>`, it already includes `<MVVM>` internally — do not double-wrap.
