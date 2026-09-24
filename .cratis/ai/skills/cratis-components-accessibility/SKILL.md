---
name: cratis-components-accessibility
description: Apply the accessibility conventions Cratis Components 4 actually implements — dialog initial-focus discipline and the armed-Enter hazard, supplying every accessible name as a localizable prop or provider message, putting the accessible name on the focusable element rather than the wrapper, keeping decorative icons hidden, and customizing through parts without fighting the component's own ARIA. Use when building or reviewing a dialog, a form control, a data table, a toast, or any component whose accessible name is currently a hard-coded English string. Do not use as a general WCAG conformance guide.
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
| `@cratis/components` | `4.6.0` | `Source/Dialogs/*` (`DialogInitialFocus`, `DialogImplementation`), `Source/CommandForm/fields/fieldAccessibility.tsx`, `Source/for_accessibility/*`, `Source/SchemaEditor/*`, `Source/ObjectNavigationalBar/*`, `Source/MIGRATION.md`, `Documentation/Styling/pass-through.md`, `Documentation/Common/cratis-components-provider.md`, `Documentation/renderers/index.md` |

Focus, overlay and collection behavior come from React Aria **internally** — the
dialog is a React Aria `Modal` inside a `ModalOverlay`; you never import React
Aria yourself, and its class names are not a styling or testing hook.

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

`DialogInitialFocus` imports from `@cratis/components/Dialogs`. While `isBusy`
is set the dialog marks its content `inert` and disables every dismissal path,
so a user cannot interact with a half-committed form.

## 2. Every accessible name is a prop or a provider message — never a baked-in English string

This is the library's strongest and most consistent convention: any string a
screen reader will read is exposed as an overridable, localizable prop, and the
library-wide ones also come from `CratisComponentsProvider`'s `messages`. The
precedence is always the same: **a named component prop wins, then the matching
provider message, then the English default.** Set the provider messages once for
the application's locale, and the per-instance props where one control needs a
different name.

| Component | Prop | Provider message |
| --- | --- | --- |
| `Dialog` / `CommandDialog` / `StepperCommandDialog` | `closeAriaLabel`; `okLabel`, `cancelLabel`, `yesLabel`, `noLabel` | `messages.dialog` (`ok`, `cancel`, `yes`, `no`, `close`) |
| `CommandStepper` / `StepperCommandDialog` | `nextLabel`, `previousLabel` | `messages.stepper` (`next`, `previous`, `submit`) |
| `DataPage` | `actionsAriaLabel`, `globalSearchAriaLabel`, `paginatorAriaLabels` | `messages.dataTable`, `messages.paginator` |
| `DataTableForQuery` / `DataTableForObservableQuery` | `paginatorAriaLabels` | `messages.paginator` (`navigation`, `first`, `previous`, `next`, `last`) |
| `Column` (filter menu) | `filterLabels` — match mode, clear, apply, true, false | `messages.columnFilter` |
| `Toaster` | `dismissAriaLabel` | `messages.notifications` (`dismiss`, `region`) |
| `Dropdown` | `aria-label`, `aria-labelledby`, `aria-describedby` | `messages.dropdown` (`showOptions`, `clearSelection`) |
| `DatePickerInput` / `CalendarField` | `aria-label` / `aria-labelledby` for the segmented **group**; `todayLabel`, `clearLabel` | `messages.datePicker` (`today`, `clear`, `openCalendar`, `previousMonth`, `nextMonth`, `label`) |
| Every CommandForm field | `aria-label` (falls back to the field `title`), `aria-describedby`, `id` | — |
| `SchemaEditor` | `labels` | — |
| `ObjectNavigationalBar` | `backLabel` — tooltip **and** accessible name | — |
| `ToolbarButton` | `title` — required; the accessible name and the tooltip | — |
| `Toolbar` | `aria-label` | `messages.toolbar` (`label`, default `'Tools'`) |
| `ToolbarFanOutItem` / `ToolbarFolder` | `tooltip` / `title` — required | — |
| `RatingField` | `starAriaLabel` | — |
| `ChipsField` | `removeAriaLabel` | — |

In a localized application, treat any component whose accessible name you have
not supplied — and any provider message group left at its English default — as
a defect.

## 3. Put the name on the focusable element, not the wrapper

An accessible name has to land on the element that actually receives focus and
carries the role. `Dropdown` demonstrates the rule: `id` and the `aria-*`
attributes route to the **trigger** (`data-cratis-part='trigger'`), not to the
layout wrapper, and the control id is deliberately not duplicated onto the
wrapper so an external `<label htmlFor>` associates correctly. A single select
follows the button/listbox pattern and a filtered one is a combobox — so in a
test, query the trigger by its accessible name or its part, **not** by
`role="combobox"`.

```tsx
<label htmlFor='role'>Advisory role</label>
<Dropdown id='role' value={role} options={roles} aria-describedby='role-help' />
```

`DatePickerInput` is a segmented field: its `id` identifies the **focus group**,
not a native text input, so name it with `aria-label`/`aria-labelledby` on the
group. `Tooltip` enhances exactly one real focusable child so focus, hover and
`aria-describedby` stay together — wrap text, fragments or several siblings in
one native control first. Several `RadioButtonField`s bound to one property need
the same explicit `name` for native arrow-key group navigation.

Apply the same rule to your own wrapper components: forward `id`, `tabIndex`,
and `aria-*` down to the focusable child rather than putting them on a
container.

## 4. Field errors are announced — do not duplicate them

Every CommandForm field runs its title and validation messages through one
helper: the control's accessible name falls back to the field `title`, and when
the field has errors a visually hidden `<span id="…-errors">` is rendered and
linked from the control's `aria-describedby` (merged with any `aria-describedby`
you pass). Arc renders the visible error text; the hidden span exists only for
assistive technology. Do **not** render a second announcement or wire your own
`aria-describedby` to the visible error — you would announce it twice.

## 5. Decorative icons are hidden

Icons and loading spinners inside a labelled control are marked
`aria-hidden='true'` so a button's accessible name is exactly its label and not
the icon's text. Do the same in application components: an icon that sits beside
a text label carries no information of its own (`<FaPlus aria-hidden='true' />`).

## 6. Customize through parts without fighting the component's ARIA

Parts (`pt` and `data-cratis-part`) are for **presentation**. The component owns
the role, the name and the state attributes on its elements: *named behavior
props override conflicting part attributes*, so a `role` or `aria-*` value you
push through `pt` onto a part the component already labels is either overridden
or produces two competing semantics. Put identity and ARIA on the documented
behavior props (§2, §3) and keep `pt` to `className`, `style` and data
attributes. State is expressed through valid ARIA plus a data attribute
(`aria-invalid` + `data-invalid`, `data-disabled`, `data-selected`) — express your
own component state the same way, never through made-up attributes.

One focus lifecycle per interactive surface: never nest a second modal or focus
trap inside a Components dialog to borrow another look, and never combine two
keyboard-selection owners for one control. A presentation adapter must preserve
the Components contract without adding a second semantic control.

## What Cratis Components does — and does not — give you

Be accurate about this — over-promising accessibility is worse than saying
nothing.

- **There is an automated scan, on representative surfaces.** The library runs
  `axe-core` (WCAG 2 A and AA rules) over a set of representative foundation
  surfaces in its own specs, and Storybook carries the accessibility addon. That
  proves those surfaces have no *automated* violations; it is not a conformance
  certification, it does not cover every component, and it cannot see keyboard
  order, focus restoration, screen-reader phrasing or color relationships the
  product introduces.
- **Modal focus management is real**: focus moves in per §1, the backdrop and the
  document behind are handled by the React Aria modal, Escape and backdrop
  dismissal follow `dismissable`, and busy content is `inert`. Verify keyboard
  order, initial focus, focus restoration and screen-reader output in the
  application's supported browsers and assistive technologies — Components does
  not certify them for you.
- **Breadcrumbs and drill-ins in `SchemaEditor`, `ObjectContentEditor` and
  `ObjectNavigationalBar` are native `<button>`s** (with `aria-current` on the
  current crumb), so they are focusable and keyboard-activatable. Anything beyond
  that — shortcut keys, arrow-key grid navigation — is not implemented; verify
  against the component source before documenting it.
- **A custom design still owns visible focus, contrast, target size, reduced
  motion, forced colors and responsive behavior.** Components supplies semantic
  behavior and the `--cratis-focus-ring` token; it cannot infer the product's
  color relationships.
- **No skip links, roving-tabindex utilities or live regions** for application
  layouts beyond the toast region (`messages.notifications.region` names it).

## Verify

- Every dialog whose confirm action is destructive or irreversible sets
  `initialFocus` to `DialogInitialFocus.Cancel` or `Content`.
- `CratisComponentsProvider` carries the locale and every `messages` group the
  application renders; no accessible name in application code is a hard-coded
  English string.
- `id`, `tabIndex`, and `aria-*` on a wrapper component reach the focusable
  child, not the container; a `DatePickerInput` is named on its group.
- Icons beside a text label are `aria-hidden`.
- No `role`/`aria-*` is injected through `pt`; no CSS or test targets a React Aria
  class name.
- Field errors are not announced twice.
- No claim of keyboard support, automated coverage, or WCAG conformance is made
  that the source does not back.
