---
agent: agent
description: Scaffold a new feature — folder structure, composition page, routing, and first slice skeleton.
---

# Scaffold a Feature

I need to scaffold a **complete new feature** in a Cratis-based project.
This creates the folder structure, composition page, and routing — ready for slices to be added.

## Inputs

- **Feature name** — PascalCase, e.g. `Projects`, `Invoices`, `UserManagement`
- **Route path** — URL path for the feature page, e.g. `/projects`
- **Navigation label** — Text shown in the sidebar/navigation, e.g. `Projects`
- **Navigation icon** — Name from `react-icons/md`, e.g. `MdFolderOpen`
- **First slice** — Optional: describe one initial slice to implement after scaffolding (can also be done later with `new-vertical-slice.prompt.md`)

## What to produce

### 1 — Feature folder

```
Features/<Feature>/
├── <Feature>.tsx          ← composition page
├── <Feature>.css          ← feature-level styles (may be empty)
└── index.ts               ← re-exports the composition page
```

### 2 — Composition page (`<Feature>.tsx`)

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from '../../Core/Page';

export const <Feature> = () => {
    return (
        <Page title="<NavigationLabel>">
            {/* Slices rendered here */}
        </Page>
    );
};
```

### 3 — Update routing

Add the feature to the application router. Locate the router configuration (typically `App.tsx` or a `routes.ts` file) and add:

```tsx
import { <Feature> } from './Features/<Feature>';

// Inside the route definitions:
{ path: '<route-path>', element: <<Feature> /> }
```

### 4 — Update navigation

Locate the navigation/sidebar configuration and add:

```tsx
import * as mdIcons from 'react-icons/md';

{
    label: '<NavigationLabel>',
    icon: mdIcons.<NavigationIcon>,
    url: '<route-path>'
}
```

### 5 — `index.ts`

```typescript
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export { <Feature> } from './<Feature>';
```

## Validation

After scaffolding, run:
- `yarn lint` — zero errors
- `npx tsc -b` — zero errors
- Confirm the new route renders an empty page without runtime errors

## Next step

Once scaffolding is complete, use `new-vertical-slice.prompt.md` to add slices to the feature.
Each slice's component will be imported and composed into `<Feature>.tsx`.
