<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/from-screenplay.md -->
# From a Screenplay model to an Arc React page

Use this when the page you are asked to build, or change, is described by a
`screen` or `form` in a `.play` model. The model is the specification; the page
is how that specification looks in React. Read the model first, build the page
to match it element by element, and report every place where it is silent or
where the request disagrees with it. Where they disagree, change the model first
(`cratis-screenplay-event-modeling`), then the page.

## What is verified, and what is not

Three different claims are mixed in any "screen to page" mapping. Keep them apart.

| Claim | Status | Evidence |
| --- | --- | --- |
| What each `screen`, `form` and interaction construct means | Verified | Screenplay `v4.64.0`: `Documentation/screenplay/{screens,forms,interactions}.md` |
| What Stage does with a screen | Verified: it draws it through Scene, it does not write a React page | Stage `v4.24.0`: `Documentation/reference/stage-rendering.md` ("It does not generate screens for it"), `Documentation/reference/default-scene-composition.md` ("does not ... generate per-screen TypeScript components"), `Source/Contracts/Scene/ScreenDirectiveConverter.cs` (`data`, `action`, `section`, `title`, `table`, `summary` become Scene elements) |
| Which Components and Arc API gives a page the same behavior | Verified API; the pairing is this skill's convention | `@cratis/components` `v4.6.0` (`DataPage`, `DataTables`, `CommandDialog`, `CommandForm`) and the sibling references |

So no product source maps a `.play` screen to an Arc React page. A hand-written
page is a **gap-fill** (`cratis-screenplay-render-and-gap-fill`) with the `.play`
screen as its contract. Screens are deferred by the backend profile: that deferral
is information `PLAY0269` and does not block binding, but the authoring
diagnostics still apply (syntax, unresolved or ambiguous references), and
warnings fail a warnings-as-errors gate. A clean compile proves the model is
authorable, not that every projection mapping resolves: binding reports `PLAY0273` for a mapped event property the event never declares. It does not prove a hand-written React page conforms to it, and no
tool checks that. You do, with the reconciliation at the end.

Stage 4.24 documents a frontend seam only for `Customizations/styles.css`
(`Documentation/guides/customize-rendered-application.md`); it documents no place
for hand-written pages in a rendered application. Never edit the generated React
scaffold. If you cannot say where the page lives, that is a blocker to report.

## Worked model

Every mapping below refers to this model. It is a complete document that compiles in design mode: the `ListBerths` list query blocks binding (V3), so it does not run through binding or rendering. The list stays because the screen is a list of berths; `GetBerth` is the keyed query for the detail screen.

```screenplay
concept BerthId : Uuid
concept BoatName : String

module Marina
  feature BerthBookings
    slice StateChange BookBerth
      command BookBerth
        berthId BerthId identifier
        boatName BoatName
        produces event BerthBooked
          boatName BoatName = boatName

    slice StateChange CancelBerth
      command CancelBerth
        berthId BerthId identifier
        produces event BerthCancelled

    slice StateView BerthList
      readmodel BerthListView
        berthId BerthId
        boatName BoatName
      projection BerthListProjection => BerthListView
        from BerthBooked
          berthId = $eventSourceId
          boatName = boatName
      query ListBerths => BerthListView[]
      query GetBerth => BerthListView optional
        by berthId BerthId

      screen BerthListScreen
        title "Berths"
        data BerthListView[] via query ListBerths
        action BookBerth
          label "Book a berth"
        action CancelBerth
        table BerthListView
          column boatName label "Boat"
          on row-click navigate to BerthDetailsScreen by berthId

      screen BerthDetailsScreen
        data BerthListView via query GetBerth by berthId
        summary BerthListView
          field boatName label "Boat"
        action CancelBerth
          navigate to BerthListScreen

  form BookBerthForm for BookBerth
    field boatName label "Boat name"
    on submit navigate to BerthListScreen
```

The form `BookBerthForm` is found through `action BookBerth` on any screen, so
one dialog component serves every screen that offers the action. It lists one
field because `berthId` is the identifier, not something the user types.

## Level 1 and 2: `screen`

| In the model | In the page | Status |
| --- | --- | --- |
| `screen BerthListScreen` | One page component named after the screen, mounted on a route of the application's router | Router is application-owned; the sandbox addresses screens as `#/<ScreenName>` and that is not a rule for your app |
| `title "Berths"` | `DataPage` `title` (required prop) | Verified |
| `data BerthListView[] via query ListBerths` | `<DataPage query={ListBerths} ...>` | Verified |
| `data ... via query Q by p` | `queryArguments={{ p: ... }}`; the value comes from a route parameter or the caller | Verified prop; the origin goes in the Step 2 table |
| `data BerthListView via query GetBerth by berthId` (single result, not a collection) | `GetBerth.use({ berthId })`, render `result.data` once `hasData`; `hasData: false` right after a command is pending, not missing ([queries-and-commands.md](queries-and-commands.md)) | Verified hook; `DataPage` is for collections |
| An observable query behind `Q` | Same `query` prop; the page stays live | Verified |
| `table BerthListView` with `column boatName label "Boat"` | `<DataPage.Columns><Column field='boatName' header='Boat' /></DataPage.Columns>`; one `Column` per modeled `column`, no extras | Verified |
| A second `table` on one screen | `DataTableForQuery` / `DataTableForObservableQuery` ([data-tables.md](data-tables.md)) | Verified |
| `summary BerthListView` with `field boatName label "Boat"` | Plain markup in the page or in `detailsComponent` (`{ item }`), one labelled line per modeled `field` | Gap: this skill's verified sources name no summary component |
| `action BookBerth` with `label "Book a berth"` | `<MenuItem label='Book a berth' icon={...} command={() => showBookBerth()} />` plus `CommandDialog` and `useDialog` | Verified |
| `action CancelBerth` on a list with a table | `MenuItem` with `disableOnUnselected`; the selected row supplies the identifier | Verified prop |
| `action` with no `label` | The model gives no text. Use the command name, spaced, and report it | Gap |
| `action X` with `navigate to S [by p]` | Navigate to the page for `S` after the command succeeds, in `onSuccess`; pass `p` from the response or the row | Assumption: `screens.md` does not say when it navigates. The form's `on submit` is defined as "after a successful submit"; confirm with the model owner |
| `on row-click navigate to S by p` | `DataPage` and `DataTableForQuery` forward no row-click prop at `4.6.0` (only `DataTableCore` has `onRowClick`, and it takes rows, not a query). Use controlled `selection` and `onSelectionChange` to navigate, or a link in a `Column` `body` | Gap: selection is not a click; report which you chose |
| `section`, `sidebar`, `main`, `template MasterDetail` | Page layout. Keep the section names as component names so the page can be compared to the model. `MasterDetail` over a list is `detailsComponent` | Convention: Components has no screen-template concept |
| `title`, `label`, `message` written as `$strings.key` | The text of that key in the `.strings` file for the locale, never retyped from memory | Verified token ([internationalization.md](https://github.com/Cratis/Screenplay/blob/v4.64.0/Documentation/screenplay/internationalization.md)); the runtime mechanism is application-owned |
| `ui profile`, `theme`, `layout`, `arrangement` | Not page code. Theming is `cratis-components-styling`; the shell is the application's | Design-only |
| A Level 3 `react` block | The block is already the implementation, with typed `Props`. Port it into a component that honors those `Props` | Convention |
| `file Screens/X.tsx` | That file is the page. The model keeps the contract; the file must still show the declared data and actions | Stage's handling of the file reference is not verified |

## `form` and `field`

| In the model | In the page | Status |
| --- | --- | --- |
| `form BookBerthForm for BookBerth` | One `CommandDialog<BookBerth>` component, reused by every screen that has `action BookBerth` | Verified (a form is discovered by its command) |
| `field boatName` | A `CommandForm` field whose `value` accessor selects `c.boatName` | Verified |
| `field dueDate label "Due date"` | The same field with `title='Due date'` | Verified |
| `field` with no `label` | The model gives no text. Use the property name, spaced, and report it | Gap |
| The property's type | Choose the field component that matches it (`InputTextField`, `NumberField`, `CalendarField`, `CheckboxField`, `DropdownField`, ...), all from `@cratis/components/CommandForm` | Verified field set |
| A command property with no `field` | Not shown. It must come from `initialValues`, the route, or the server. If nothing supplies a required value, report a model gap; do not add a field | Convention |
| `populate from item` | `initialValues` built from the selected row, synchronously | Verified prop |
| `populate via query Q by p` | Read `Q`, then pass the result as `currentValues` (the overlay for late-loading values) | Verified prop |
| `field p from src` | Keep the accessor on `p`; seed `p` from `src` in `initialValues` | Convention |
| `field p compose using Callback` | The value is derived. Put the derivation in a pure function or view-model getter with a spec, and seed it through `initialValues`/`currentValues` | Gap: the model names a callback but not its signature; report it |
| `on submit navigate to S [by p]` | `onSuccess` navigates. `onSuccess` fires only after the command succeeds | Verified |
| No `on submit` | A successful submit leaves the user where they were: close the dialog, nothing more | Verified |

The command's validation lives on the command, not in the form. The page
surfaces it through the bound fields and the failure callbacks; it does not
restate a rule ([queries-and-commands.md](queries-and-commands.md)).

## Interactions: `on`, `uses`, `behavior`

The action set is closed ([interactions.md](https://github.com/Cratis/Screenplay/blob/v4.64.0/Documentation/screenplay/interactions.md)).

| Model action | Page |
| --- | --- |
| `execute <Command>` | `CommandDialog`, or `Command.use()` then `execute()` for a command with no user input ([queries-and-commands.md](queries-and-commands.md)) |
| `confirm "text"` | `useConfirmationDialog`; declining stops what follows |
| `notify info\|warning\|error "text"` | `toast.info/warn/error({ title, description })`, or `toastCommandResult` for a command result |
| `refresh <Query>` | For a query you read with the hook, call the `perform` function from `[result, perform, setSorting]` ([queries-and-commands.md](queries-and-commands.md)). An observable query updates by itself and has no `perform`. Gap: `DataPage` only forwards `onRefresh` to your own callback at `4.6.0`; it does not re-run its internal snapshot query, so an explicit refresh of a `DataPage` list has no verified mechanism. Use an observable query, or read the query yourself and render with `DataTableCore`-level components, and report the choice |
| `open dialog <Template>` / `close dialog` | `useDialog` / `closeDialog(DialogResult.Ok)` |
| `navigate to <Screen>` / `navigate back` | The application's router |
| `set <target> to <value>` | View-model state |
| `on success` / `on failure` continuation | `onSuccess` / `onFailed` |
| `raise <ApplicationTrigger>` | No page equivalent; a trigger is consumed by a `reaction` on the backend |

Triggers map the same way: `on click` is the control's handler or a `MenuItem`
`command`; `on select` is `onSelectionChange`; `on enter` is page mount or the
view model's `handleParams`.

`on submit` has two meanings. The one-line form shorthand `on submit navigate to
<Screen>` runs after a successful command, so it belongs in `onSuccess`. A generic
`on submit` behavior runs when the form is submitted and passes the modeled
validation, and it may itself contain `execute`. Run its actions from the
validated submit handler, in the order written; use `onSuccess` and `onFailed`
only for the `on success` and `on failure` continuations of an `execute` inside it.

`on event <Event>` is a gap. It observes a domain event and carries that event's
payload; an observable query delivers read-model changes, not event payloads, so
it is not a substitute. Do not implement it until the payload, subscription and
teardown contract of a verified event-delivery adapter is known. Report it.
`on interval` and `raise` need the same decision.

Anything a page does that no `on` clause says (an extra confirmation, an extra
toast) is an addition the model lacks: either add it to the model or leave it
out.

## Reconcile before you finish

Walk the screen once, declaration by declaration, and fill this table in your
report. Both columns must be empty at the end, or the entry explained.

| Model element | Page counterpart | Status |
| --- | --- | --- |
| each `data`, `action`, `column`, `summary field`, `form field`, `on` clause | the component or prop that implements it | present / missing / gap (see above) |
| (each user-visible value or control in the page) | the model element it came from | traced / **not in the model** |

A page element that is not in the model is not allowed to stay silently. Remove
it, or change the model first and say so.

## Verify

- The provenance of this guidance is in [provenance.md](provenance.md).
- The `.play` screen and form were read before the page was written, and the
  mapping gaps above are named in the report with the choice made.
- Every `data`, `action`, `column`, `summary field`, form `field` and `on` clause
  has a counterpart; the page adds no data or action the model lacks.
- Labels and messages equal the model's text or its `$strings` key.
- No generated or Stage-managed file was edited; the page's location is known.
- The states of [SKILL.md](../SKILL.md) Verify (empty, loading, failure, denied,
  after success) are designed.
