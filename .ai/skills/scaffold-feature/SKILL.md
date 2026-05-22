---
name: scaffold-feature
description: Use this skill when asked to create a new feature, section, or page that does not yet exist in a Cratis-based project. Sets up the folder, composition page, routing, and navigation entry before any slices are added.
---

Scaffold a brand-new feature folder with routing and navigation — ready for slices.

## What to produce

### 1 — Feature folder

```
Features/<Feature>/
├── <Feature>.tsx    ← composition page
├── <Feature>.css    ← feature-level styles (can be empty initially)
└── index.ts         ← re-exports the composition page
```

### 2 — Composition page (`<Feature>.tsx`)

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from '../../Core/Page';

export const <Feature> = () => {
    return (
        <Page title="<NavigationLabel>">
            {/* Slices will be composed here */}
        </Page>
    );
};
```

### 3 — `index.ts`

```typescript
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export { <Feature> } from './<Feature>';
```

### 4 — Update routing

Locate the app router (typically `App.tsx` or a `routes.ts` file) and add:

```tsx
import { <Feature> } from './Features/<Feature>';

{ path: '<route-path>', element: <<Feature> /> }
```

### 5 — Update navigation

Locate the sidebar/navigation configuration and add:

```tsx
import * as mdIcons from 'react-icons/md';

{
    label: '<NavigationLabel>',
    icon: mdIcons.<NavigationIcon>,
    url: '<route-path>'
}
```

## Rules

- Feature name: PascalCase (e.g. `Projects`, `Invoices`)
- Route path: kebab-case (e.g. `/projects`, `/user-management`)
- Navigation icon: from `react-icons/md` — e.g. `MdFolderOpen`, `MdPeople`
- Copyright header on every file

## Validation

Run `yarn lint` and `npx tsc -b`. Fix all errors. Confirm the blank page renders without runtime errors.

## Next step

Add slices using the `new-vertical-slice` skill.
