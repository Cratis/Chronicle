<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/mvvm.md -->
# MVVM reference

The Arc MVVM layer keeps page logic in plain TypeScript classes and leaves the
component declarative. `@cratis/arc.react.mvvm` builds on tsyringe for
injection and MobX for reactivity.

## When to use it

Extract a view model as soon as a component has three or more `useState` calls,
any state-synchronizing `useEffect`, derived values computed from other state,
or state shared by prop drilling. A trivial presentational leaf needs none.

## Setup

The app needs `experimentalDecorators`, `emitDecoratorMetadata`, and
`reflect-metadata` wired for the decorators to work.

Wrap the app (or the route root) in the MVVM provider — it configures MobX and
registers the shared bindings:

```tsx
import { MVVM } from '@cratis/arc.react.mvvm';

<MVVM>
    <App />
</MVVM>
```

`<Arc>` does **not** include `<MVVM>`; both are needed.

The provider's initialization registers `IMessenger`, `INavigation`,
`IIdentityProvider`, and `ILocalStorage` into the root container.

## The view model

```ts
import { injectable } from 'tsyringe';

@injectable()
export class AccountsViewModel {
    selected: AccountSummary | null = null;

    select(account: AccountSummary | null) { this.selected = account; }

    get selectedName() { return this.selected?.name ?? ''; }
}
```

- `@injectable()` is tsyringe's — there is no Cratis-specific view-model
  decorator.
- **`withViewModel` applies `makeAutoObservable` for you.** Do not call it in
  the constructor and do not decorate plain assignments.
- Derived state is a getter, not a `useMemo` in the component.
- No JSX, no React state, no React hooks in a view model — inject the
  abstraction instead.
- A non-trivial view model must be constructible directly in a spec.

## `withViewModel`

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';

export const AccountsPage = withViewModel(AccountsViewModel, ({ viewModel, props }) => (
    /* read viewModel state inside JSX; call viewModel methods from handlers */
));
```

The render function receives `{ viewModel, props }`. Behind the scenes
`withViewModel` creates a child DI container per component instance, registers
the props, route parameters, query parameters, dialog context, command scope,
query scope, messenger, and dialogs into it, resolves the view model, makes it
observable, and renders the body inside an observer.

Any query, observable query, or command resolved through that container is
automatically configured with the microservice, API base path, and origin from
the Arc context.

### Dereference late

Read `viewModel.property` **inside** JSX. Destructuring an observable at the top
of the component body captures the value and stops tracking changes.

`useCallback` and `useMemo` are unnecessary inside a `withViewModel` component —
the render is already wrapped in an observer, and their presence signals state
that belongs in the view model.

When passing an observable array into a raw non-observer child, materialize it
with `.slice()`. In practice prefer the observable data table, which handles
reactivity internally.

## Props, route parameters, and query parameters

| Need | Mechanism |
| --- | --- |
| Props set once at mount | `@props readonly componentProps: TProps` |
| Props that change after mount | implement `IHandleProps<TProps>` → `handleProps(props)` |
| Route parameters | `@params readonly routeParams: RouteParams` |
| Route parameters that change | implement `IHandleParams<T>` → `handleParams(params)` |
| Query-string parameters | `@queryParams readonly query: QueryParams` |
| Query parameters that change | implement `IHandleQueryParams<T>` → `handleQueryParams(queryParams)` |

The handler method names are `handleProps`, `handleParams`, and
`handleQueryParams`. Each is invoked on mount and again whenever the incoming
value actually differs.

```ts
@injectable()
export class AccountDetailsViewModel implements IHandleProps<AccountDetailsProps> {
    account: AccountSummary | null = null;

    handleProps(props: AccountDetailsProps) { this.account = props.account; }
}
```

Route and query parameters arrive from the URL as strings. `withViewModel`
deserializes them into the class you declared on the decorated constructor
parameter, so declare a real parameter class with typed properties rather than
an inline object type — that class is what drives the conversion.

## Injectable abstractions

Never touch a browser or React global from a view model.

| Abstraction | Package | Replaces |
| --- | --- | --- |
| `IMessenger` | `@cratis/arc.react.mvvm/messaging` | cross-component selection, not React context |
| `IDialogs` | `@cratis/arc.react.mvvm/dialogs` | imperative confirmation and busy dialogs |
| `IIdentityProvider` | `@cratis/arc/identity` | identity, instead of the `useIdentity` hook |
| `INavigation`, `ILocalStorage` | `@cratis/arc.react.mvvm/browser` | URL navigation, `localStorage` |
| `IViewModelDetached` | `@cratis/arc.react.mvvm` | teardown via `detached()` |

`IDialogs` exposes `show(input)`, `showConfirmation(title, message, buttons)`
returning a `DialogResult`, and `showBusyIndicator(title, message)` returning a
busy indicator with a `close()` method. For a user-triggered dialog that opens
a React command dialog, use `useDialog` in the component instead.

`IViewModelDetached.detached()` runs on unmount — unsubscribe subscriptions and
clear timers there.

## `observer`

Import `observer` from `@cratis/arc.react.mvvm`, never directly from
`mobx-react`. The package re-exports it deliberately so the observer boundary
stays a single, swappable dependency.

## Testing

A view model is a plain class: construct it directly, pass small typed fakes or
stubs for its dependencies, and assert on state transitions and getters. See the
**cratis-application-react-specifications** skill.
