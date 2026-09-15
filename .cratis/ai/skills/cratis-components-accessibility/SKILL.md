---
name: cratis-components-accessibility
description: Apply the accessibility conventions Cratis Components actually implements — dialog initial-focus discipline and the armed-Enter hazard, supplying every accessible name as a localizable prop, putting the accessible name on the focusable element rather than the wrapper, and using the PrimeReact pass-through prop to strip invalid ARIA. Use when building or reviewing a dialog, a form control, a data table, a toast, or any component whose accessible name is currently a hard-coded English string. Do not use as a general WCAG conformance guide.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-accessibility/SKILL.md -->

# Accessibility in Cratis Components

This skill covers what `@cratis/components` **actually implements and enforces**
in its own source. It is deliberately narrow. It is not a WCAG conformance
guide, and it does not claim the library is conformant.

Read the honest boundary at the end before promising a user any behavior beyond
what is written here.

## Verified product sources

| Package | Version | Verified from |
| --- | --- | --- |
| `@cratis/components` | `3.0.0` | its component sources and specifications |
| `primereact` | `^11.0.0` | peer of `@cratis/components@3.0.0` |

## 1. Dialog focus discipline

A modal must move focus into itself when it opens. Leaving focus on the document
body strands keyboard and screen-reader users, so `Dialog` has **no "focus
nothing" option** — `DialogInitialFocus` has exactly three members and one of
them always applies.

```ts
enum DialogInitialFocus { Confirm = 1, Cancel = 2, Content = 3 }
```

| Member | Effect | Use for |
| --- | --- | --- |
| `Confirm` | focuses the confirm button — **the default** | ordinary, safe dialogs |
| `Cancel` | focuses the least destructive action | anything irreversible |
| `Content` | focuses the dialog title, arming nothing | a dialog that should commit to nothing |

### The armed-Enter hazard

`Confirm` *arms* the confirm button. A browser fires `click` from the `keydown`
of Enter or Space, so an Enter that is still held — or repeating — from the
action that opened the dialog can confirm it immediately, before the user has
read anything.

**Any dialog whose confirm action is destructive or irreversible must set
`initialFocus`.**

```tsx
<CommandDialog
    command={DeleteAccount}
    title='Delete account'
    okLabel='Delete'
    initialFocus={DialogInitialFocus.Cancel}>
```

`Cancel` degrades to `Content` automatically when the chosen button set has
nothing to dismiss with, so it is always safe to ask for.

`DialogInitialFocus` imports from `@cratis/components/Dialogs`.

## 2. Every accessible name is a prop — and never a baked-in English string

This is the library's strongest and most consistent convention: any string a
screen reader will read is exposed as an overridable, localizable prop. Set it.

| Component | Prop |
| --- | --- |
| `Dialog` (and `CommandDialog`) | `closeAriaLabel` — default `'Close'` |
| `DataPage` | `actionsAriaLabel` — default `'Actions'` |
| `DataTableForQuery` / `DataTableForObservableQuery` | `paginatorAriaLabels` |
| `Column` (filter menu) | `filterLabels` — trigger name, clear, apply, true, false |
| `Toaster` | `dismissAriaLabel` — default `'Dismiss'` |
| `Dropdown` | `aria-label`, `aria-labelledby`, `aria-describedby` |
| `SchemaEditor` | `labels` — ten strings, five of them accessible names |
| `ObjectNavigationalBar` | `backLabel` — tooltip **and** accessible name |
| `ToolbarButton` | `title` — required; the accessible name and the tooltip |
| `ToolbarFanOutItem` | `tooltip` — required |
| `RatingField` | `starAriaLabel` |
| `ChipsField` | `removeAriaLabel` |

Where the library can resolve a name from a locale it does — the table
paginator reads its navigation labels from the PrimeReact locale rather than
hard-coding them, and the busy-indicator dialog names its progress indicator
from the consumer-supplied, already-localized message. Follow that pattern:
**never bake an English accessible name into application code.**

In a localized application, treat any component whose accessible name you have
not supplied as a defect.

## 3. Put the name on the focusable element, not the wrapper

An accessible name has to land on the element that actually receives focus and
carries the role. `Dropdown` demonstrates the rule: `id`, `tabIndex`, and all
three ARIA attributes route to the combobox trigger, not to the layout wrapper,
and the control id is deliberately not duplicated onto the wrapper so an
external `<label htmlFor>` associates correctly.

```tsx
<label htmlFor='role'>Advisory role</label>
<Dropdown id='role' value={role} options={roles} aria-describedby='role-help' />
```

Apply the same rule to your own wrapper components: forward `id`, `tabIndex`,
and `aria-*` down to the focusable child rather than putting them on a
container.

## 4. Decorative icons are hidden

Icons and loading spinners inside a labelled control are marked
`aria-hidden='true'` so a button's accessible name is exactly its label and not
"pi pi-plus Add account". Do the same in application components: an icon that
sits beside a text label carries no information of its own.

## 5. Use pass-through to strip invalid ARIA

Setting a pass-through value to `undefined` **removes** that attribute. Cratis
Components uses this deliberately where PrimeReact 11 emits ARIA that is invalid
or points at ids it never renders — for example removing `aria-sort` from a sort
control that carries `role="button"`, and removing `role` and `aria-controls`
from stepper headers whose referenced panels have no matching ids.

```tsx
pt={{ root: { role: undefined, 'aria-controls': undefined } }}
```

Reach for this when an upstream primitive emits an attribute that is wrong for
the element it lands on. Removing a broken role usually leaves a natively
keyboard-accessible element behind, which is better than a role that lies.

## 6. Prefer an invalid state over an invalid attribute

Where a control needs to express invalidity, the library translates it to
`aria-invalid` plus a data attribute rather than forwarding an attribute the DOM
would reject. Follow that: express state through valid ARIA, not through
made-up attributes.

## What Cratis Components does **not** give you

Be accurate about this — over-promising accessibility is worse than saying
nothing.

- **No accessibility linting or scanning is configured.** The library's ESLint
  configuration has no `jsx-a11y` plugin, and no axe or Storybook
  accessibility addon is installed. There is **no automated accessibility gate**
  in the quality gates. Any accessibility claim comes from a manual audit.
- **The schema editors, the object content editor, and the object navigational
  bar are click-only.** Their breadcrumbs and drill-in affordances are plain
  elements with click handlers — no role, no `tabIndex`, no key handling. Do not
  document keyboard navigation for them.
- **Field-level error text is not wired to its input.** A validation message
  shown beside a field is not associated through `aria-describedby` or
  `aria-errormessage`. If your application needs that association, add it in the
  application component.
- **There is no focus trap, roving tabindex, skip link, or live region** beyond
  what the dialog and the PrimeReact primitives provide.
- Some existing Cratis Components documentation describes keyboard shortcuts and
  ARIA support for the schema editor and the navigational bar that the code does
  not implement. Verify against the component source before repeating any such
  claim.

## Verify

- Every dialog whose confirm action is destructive or irreversible sets
  `initialFocus` to `DialogInitialFocus.Cancel` or `Content`.
- No accessible name in application code is a hard-coded English string — each
  comes from the component's label prop, resolved from the application's
  locale.
- `id`, `tabIndex`, and `aria-*` on a wrapper component reach the focusable
  child, not the container.
- Icons beside a text label are `aria-hidden`.
- Any ARIA attribute removed from an upstream primitive is removed through
  pass-through `undefined`, with a comment saying why.
- No claim of keyboard support, automated accessibility scanning, or WCAG
  conformance is made that the source does not back.
