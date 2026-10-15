---
name: cratis-components-common-controls
description: Choose and bind Cratis Components ComboBox, ToggleGroup, Tabs, TagGroup and Breadcrumbs with controlled values, query states, keyboard semantics and localizable names. Use for entity pickers, exclusive choices, panel tabs, tag inputs or navigation trails. Do not use for Arc generated command fields, data tables, canvas palettes or theming.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-common-controls/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Common controlled controls

## Verified product sources

All claims and prop shapes are from **Cratis/Components v4.26.4**, source rather than unversioned examples.

| Package | Version | Sources |
| --- | --- | --- |
| `@cratis/components` | `4.26.4` | `Source/Common/ComboBox.tsx:44-161,223-295`; `ToggleGroup.tsx:21-87,109-145`; `Tabs.tsx:23-79,91-132`; `TagGroup.tsx:49-145,155-260`; `Breadcrumbs.tsx:23-61,81-126` |
| React | `^19.0.0` peer | `Source/package.json:319-340` |

Imports are exported by `Source/Common/index.ts:11-15`; `Source/package.json:95-99` exposes the `Common` subpath. Use **cratis-components-styling** for the provider and CSS setup and **cratis-components-accessibility** for naming/focus. These controls do not replace Arc's generated `InputTextField`/other command fields; use **cratis-arc-react-page** for those bindings.

## Choose the semantic control first

| Need | Control | Controlled contract |
| --- | --- | --- |
| Pick one option, with search | `ComboBox` | `options: { key, label, description?, disabled? }[]`, `value: string \| null`, `onChange(key)` |
| Choose one value from a small set | `ToggleGroup` | `options: { value, label, icon?, disabled? }[]`, `value: string`, `onChange(value)` |
| Reveal a corresponding content panel | `Tabs` | `tabs: { id, label, content, disabled? }[]`, `value: string`, `onChange(id)` |
| Add/remove string values | `TagGroup` | `value: string[]`, `onChange(nextValues)` |
| Show where the page sits in navigation | `Breadcrumbs` | `items: { label, href?, onNavigate? }[]`, outermost first |

`ToggleGroup` is a single-selection value choice with radio-group semantics and one Tab stop. `Tabs` supplies real panels and tab/panel relationships. Do not use panel-less tabs as a fancy radio group (`ToggleGroup.tsx:78-87`, `Tabs.tsx:70-79`).

## 1. Bind a picker, not its display text

A controlled presentation component; the caller owns the selected key and supplies approved/localized text. `inputValue` is a separate search draft, not the selected value.

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComboBox, type ComboBoxOption } from '@cratis/components/Common';

interface AccountPickerProps {
    options: ComboBoxOption[];
    value: string | null;
    onChange: (key: string | null) => void;
    label: string;
    loading: boolean;
    loadingMessage: string;
    emptyMessage: string;
}

export const AccountPicker = (props: AccountPickerProps) => (
    <ComboBox
        options={props.options}
        value={props.value}
        onChange={props.onChange}
        aria-label={props.label}
        loading={props.loading}
        loadingMessage={props.loadingMessage}
        emptyMessage={props.emptyMessage}
        openOnFocus
    />
);
```

The host loads options; ComboBox never fetches them. For server-filtered results, supply `inputValue`, `onInputChange` and `filter='none'`; the caller debounces/cancels requests and discards stale answers. Local `filter` defaults to `'contains'`; `'startsWith'` is also supported. Explicit open/focus shows all options rather than filtering to the selected label (`ComboBox.tsx:170-177,223-266`).

Useful props (`:106-161`):

- `failure: ReactNode` replaces options with failure content. `loading` shows `loadingMessage`; `emptyMessage` is for a genuine empty result. Do not map denied/failed loads to an empty list without an explanation.
- `optionLayout='inline' | 'stacked'` controls secondary `description` placement.
- `action={{ label, onAction }}` offers a footer action receiving the current input text. It **does not select or create an option for you**. The handler may return a promise, but the component fires it without awaiting; the caller owns busy/error handling (`:268-287`).
- `required`, `invalid`, `errorMessage`, `description`, `disabled`, `readOnly`, `name` and `id` cover field state. `id` belongs to the input for `<label htmlFor>`. `name` carries the selected key, not search text (`:133-156`).

## 2. Choose a value or a panel

JSX binding excerpts inside an existing view. They assume caller-owned string values, change callbacks, and localized `labels` with the named properties:

```tsx
import { ToggleGroup, Tabs } from '@cratis/components/Common';

<ToggleGroup
    aria-label={labels.viewMode}
    value={viewMode}
    onChange={changeViewMode}
    options={[
        { value: 'compact', label: labels.compact },
        { value: 'expanded', label: labels.expanded }
    ]}
/>

<Tabs
    aria-label={labels.details}
    value={selectedTab}
    onChange={changeTab}
    tabs={[
        { id: 'overview', label: labels.overview, content: overview },
        { id: 'history', label: labels.history, content: history }
    ]}
/>
```

`ToggleGroup.onChange` receives a string, not a DOM event; selecting the already-selected value is ignored. `Tabs.onChange` also receives an ID string. Both offer `disabled`, accessible labeling, `className` and typed `pt` parts. ToggleGroup additionally exposes `invalid` and `aria-describedby` (`ToggleGroup.tsx:52-76,116-121`; `Tabs.tsx:51-68`). Do not infer route navigation from changing a tab; its job is revealing content.

## 3. Add tags without confusing display with read-only

JSX excerpt in a view with caller-owned `tags: string[]`, `changeTags`, and localized labels:

```tsx
import { TagGroup } from '@cratis/components/Common';

<TagGroup
    aria-label={labels.tags}
    value={tags}
    onChange={changeTags}
    separators={[',', ';']}
    removeLabel={tag => labels.removeTag.replace('{tag}', tag)}
/>
```

TagGroup defaults to `editable=true`, comma separators and `allowDuplicates=false`. Enter or separators commit trimmed nonempty values; paste splits on separators. Empty-entry Backspace removes the last tag (`TagGroup.tsx:94-109,127-145,190-260`).

**`editable=false` only removes the adding input; existing tags remain removable.** Use `disabled` when removal must also be unavailable, or a display-only surface for noninteractive tags. Supply `removeLabel(value)` for localized accessible names. `removeIcon` overrides that group's glyph (ahead of provider `icons.remove`) without changing the button's name or keyboard behavior (`TagGroup.tsx:54-91,155-190`). Prefer unique values: removal is value-keyed, so this is not an identity-preserving collection editor (`:139-140`).

## 4. Build the breadcrumb trail from actual navigation

JSX excerpt in a view whose `labels` are localized:

```tsx
import { Breadcrumbs } from '@cratis/components/Common';

<Breadcrumbs
    aria-label={labels.location}
    items={[
        { label: labels.home, href: '/' },
        { label: labels.accounts, href: '/accounts' },
        { label: labels.currentAccount }
    ]}
/>
```

Earlier entries can supply `href` or router-owned `onNavigate`. The last item is current: its destination/callback is suppressed and React Aria supplies `aria-current='page'`. Separators are decorative; `separator` accepts any React node (`Breadcrumbs.tsx:97-126`). Supply a distinct landmark name if a page has multiple trails. Breadcrumbs do not discover routes or enforce access.

## Verify the binding

Test caller state changes using the real payload shapes (key/string/string array), not invented `{ value }` events. Exercise null selection, query failure, disabled options, panel content, tag removal and the final breadcrumb. Browser-check keyboard/focus and accessible names. `pt` keys belong to each exported `*Parts` interface; style those parts rather than overriding internal React Aria markup. Do not add undocumented props to work around a failed interaction.
