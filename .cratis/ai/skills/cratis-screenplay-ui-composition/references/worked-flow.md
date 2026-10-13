<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/worked-flow.md -->
# Worked flow: marina berth bookings

Complete document. It compiles and binds (screens, forms and descriptions are informational
PLAY0269/0270; the `ListBerths` list query binds since Screenplay 4.105.0, and Stage 4.73.1 would render only
an `observable` list). Used by `SKILL.md`, *Compose in this order*. Opening an input screen is a
click (`section ... on click navigate to`); the input screen's `action` carries the destination
**after success**.

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
      screen BookBerthScreen
        title "Book a berth"
        data BerthListView[] via query ListBerths
        action BookBerth
          navigate to BerthListScreen
    slice StateView BerthList
      readmodel BerthListView
        berthId BerthId
        boatName BoatName
      projection BerthListProjection => BerthListView
        from BerthBooked
          berthId = $eventSourceId
          boatName = boatName
      query ListBerths => BerthListView[]
      screen BerthListScreen
        data BerthListView[] via query ListBerths
        section bookBerthInput
          title "Book a berth"
          on click
            navigate to BookBerthScreen

  form BookBerthForm for BookBerth
    field berthId label "Berth"
    field boatName label "Boat name"
    on submit navigate to BerthListScreen
```

## Field trace

| Where | Field | Origin | Status |
| --- | --- | --- | --- |
| `BookBerthForm` | `berthId` | `BookBerth.berthId`, typed by the user | traced |
| `BookBerthForm` | `boatName` | `BookBerth.boatName` | traced |
| `BerthBooked` | `boatName` | `BookBerth.boatName` (declared payload) | traced |
| `BerthListView` | `boatName` | `BerthBooked.boatName` | traced |
| `BerthListView` | `berthId` | event source id of `BerthBooked`, which is `BookBerth.berthId` | traced |

Command to event to read model to screen: each hop is declared. A projection cannot read a
payload field the event never declares, so the event payload line matters.

## Notes

- `identifier` marks which property names the event source. It neither generates the value nor
  fills it. Here the user supplies the berth. Other sources are a prefill (`populate via query`
  or `populate from item` when the user chose a berth on the previous screen) or a modeled
  selection. `generated identifier` is ESM v7: it binds on standalone Screenplay 4.68.0 and later and on the current
  cratis bundle (`PLAY0268` on the 4.66.0 bundled in `cratis` 3.28.x); CLI 3.57.0 / Stage 4.89.1 render supported UUID/String-concept generated
  identifiers; unsupported generated/result combinations remain refused (command-surface
  `references/generated-values-and-responses.md`).
- The form is found through the input screen's `action BookBerth`; neither screen names it. A click
  opens `BookBerthScreen` before the command runs; the action's `navigate to` is where the user
  lands after success, never a route to the command's own input screen (Screenplay `screens.md`).
- Compiled with `screenplay` 4.139.0 `--warnaserror`; `cratis screenplay validate --executable` 3.53.1
  reports it valid (bundled 4.137.0).
