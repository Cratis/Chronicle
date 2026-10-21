<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/flow-review.md -->
# Flow review (text form)

Use this before any layout work (`SKILL.md`, *Compose in this order*, step 2). It needs
no board: the `.play` source and your own words are enough. If the host offers the `screenplay` MCP server's
`visualize-model` (MCP-Apps hosts only) you may add a board view; it never replaces this.

## Procedure

1. List the screens of the flow in the order a user meets them. Take them from the
   model, not from memory: read the `screen` declarations first.
2. Write one entry per screen in the shape below.
3. Walk each `action ... navigate to` and each `on submit navigate to`: the target
   screen must exist and the entry for it must say how the user arrived.
4. Mark gaps instead of filling them: a field with no origin, a screen with neither
   `data` nor `action`, an action nobody reaches.
5. Ask the person owning the flow to confirm or correct the entries. Only then move to
   templates, forms and interactions.

## Entry shape

```text
<Screen>
  Shows:      <read model and the fields that matter, in plain words>
  Arrived by: <action, navigation or entry point>
  Can do:     <intents; each one a command or a navigation>
  Gaps:       <none, or what has no origin / no target>
```

Two or three sentences per screen is enough. Name states the screen must cope with
(empty list, command refused, caller not allowed) so that nobody discovers them in
the build.

## Example (marina bookings)

```text
BerthListScreen
  Shows:      the berths booked so far (berth, boat name).
  Arrived by: the entry point, and after a booking is submitted.
  Can do:     open the booking screen (a click on the booking section opens BookBerthScreen).
  Gaps:       none. Empty list: nothing booked yet.

BookBerthScreen
  Shows:      the same berth list for context.
  Arrived by: "book a berth" on BerthListScreen.
  Can do:     enter a boat name and submit BookBerth; then return to BerthListScreen.
  Gaps:       none. Refusal state (berth already taken) not yet modeled: ask.
```

## Field trace table

| Where | Field | Origin | Status |
| --- | --- | --- | --- |
| `BookBerthForm` | `boatName` | `BookBerth.boatName` | traced |
| `BerthListScreen` | berth, boat name | `BerthListView` properties | traced |

A row whose origin is blank is a model gap, not a screen detail.
