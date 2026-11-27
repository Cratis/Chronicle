# @cratis/chronicle-ui

Reusable parts of the Cratis Chronicle Workbench for React hosts. This first release is deliberately small:

| Entry | Provides |
| --- | --- |
| `@cratis/chronicle-ui` | `Card` and `CardProps` - the Workbench card with optional header and footer slots |
| `@cratis/chronicle-ui/event-stores` | `useEventStoreNames()` - the live list of event store names, from the same generated query the Workbench uses |
| `@cratis/chronicle-ui/styles.css` | The card's scoped styles |

Nothing else is exported. The Workbench shell, routing, dialogs, the Monaco editor and its worker, charts, authentication UI and the other proxies stay in the Workbench; they move here only when an extraction needs them.

## Install

```sh
npm install @cratis/chronicle-ui
```

The package is ESM only and bundles none of its host dependencies. Install these peers (the host owns one instance of each):

| Peer | Range |
| --- | --- |
| `react`, `react-dom` | `^19.3.1` |
| `@cratis/arc`, `@cratis/arc.react` | `^23.21.0` |
| `@cratis/components` | `^4.63.0` |
| `@cratis/fundamentals` | `^7.34.3` |
| `reflect-metadata` | `0.2.2` |

## Host setup

The host already provides what the Workbench provides. Import `reflect-metadata` once before the event store entry, mount the Components and Arc providers the way any Cratis application does, and import the Components styles before this package's styles:

```tsx
import 'reflect-metadata';
import '@cratis/components/styles';
import '@cratis/chronicle-ui/styles.css';

import { Card } from '@cratis/chronicle-ui';
import { useEventStoreNames } from '@cratis/chronicle-ui/event-stores';

export const EventStores = () => {
    const names = useEventStoreNames();
    return names.map(name => <Card key={name} header={name}>...</Card>);
};
```

`Card` has no text of its own; labels come from the host. `useEventStoreNames()` calls the generated Arc observable query through the host's query context: connection, authentication, loading and error behavior are the host's, and the hook returns `[]` until the first result arrives.

## Compatibility and release

The package is released with Chronicle and carries the Chronicle version. Its public surface follows semver: adding an export is a minor change; removing or changing one, or raising a peer range past what an existing host can provide, is a major change. The event store names query is generated from the Chronicle server contract, so the hook works against the server of the same release.

## Developing in this repository

The package is a workspace of the private Workbench project (`Source/Workbench`). Run these from `Source/Workbench`:

```sh
yarn build:ui                                     # stage generated proxies, build dist/
yarn workspace @cratis/chronicle-ui build:watch   # rebuild dist/ on change while a Workbench dev server runs
yarn workspace @cratis/chronicle-ui check:consumers  # pack, install the tarball into a temporary host, type-check and build it
```

The Workbench resolves this package through its `dist/`, so `yarn dev`, `yarn build`, `yarn test` and the Storybook scripts run `build:ui` first. The generated query proxies stay where the Chronicle proxy generator writes them (`Source/Workbench/Features/EventStores`); `build:ui` copies exactly the two it needs, unmodified, into the ignored `generated-input/` folder, and fails if either is missing or no longer marked as generated. Do not edit `generated-input/` or `dist/`.
