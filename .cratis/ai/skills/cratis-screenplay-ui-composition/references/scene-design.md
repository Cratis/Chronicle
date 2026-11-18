<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/scene-design.md -->
# Scene Design: designing screens as an agent

## Contents

- What Scene Design is and what it never does
- The loop
- Ground every choice in the catalogue
- Design operations
- Checks and refusals
- Free styling is refused
- Proposals into the design-system draft
- Screens, free screens and the session
- A worked turn
- Checklist

Requires the Scene Design packages, `@cratis/scene.design` (TypeScript) and `Cratis.Scene.Design` (C#), which share one catalogue, one operation vocabulary and one set of fixtures. The tools an agent calls are whatever the host registers: resolve their native names from the connected tool list and read their schemas instead of assuming the names below. This page describes the contract every host's tools follow.

## What Scene Design is and what it never does

Scene Design sits between a designer (a person on a canvas or an agent with tools) and the Scene document, so that a design is always **made of what the design system and the component packages offer**. Screenplay text stays the source of truth: a designed screen is a [free screen](free-screens.md), and each revision of it is stored as Screenplay text.

- It never reads or writes Screenplay itself; the host converts text to a Scene document and back.
- It never owns commands, authorization or event streams; the host does.
- It never lets a design invent a component, variant, token, template, property or value.

Syntax for the screens a design produces: [free-screens.md](free-screens.md). The design system that grounds them: [design-system-and-themes.md](design-system-and-themes.md).

## The loop

A design turn is a tool loop run by the host, not a single answer:

1. **Read.** Ask the catalogue what exists instead of recalling it. Questions are answered from the catalogue and never contain anything it does not list.
2. **Write.** Create a screen as whole Screenplay text, or change one with typed design operations, or ask for variations or tweaks.
3. **Take the diagnostics.** Every write runs in a scratch session through the Screenplay compiler and the catalogue checks. The diagnostics or the refusal are the tool result.
4. **Repair inside the loop.** Read the refusal, take one of its alternatives or fix the text, and write again. A refused operation changes nothing, and the operations after it still run against the design as it then is.
5. **Finish.** End the turn with a short reply for the person. The host applies the result as a new revision of each touched screen (one turn id) and a revert is one action.

Do not finish with a design that still carries error diagnostics, and do not report an operation as applied unless its outcome says `applied`.

## Ground every choice in the catalogue

The catalogue is built once, deterministically, from the resolved design system, its themes, the UI profile, the packages' component descriptors, the screen templates and, when the host supplies them, shapes, queries, commands and sample data. Read it on demand, one question at a time:

| Ask about | Answers |
| --- | --- |
| components (filtered by category, by the outlet or template they fit, or searched) and one component | properties with kinds and choices, placement, the variants and states the design system also declares |
| templates and one template | outlets and what each takes |
| tokens and one token | the value per theme and mode |
| the design system's components, themes, the profile, words | what the product declares |
| shapes, queries, commands | what a screen may bind to |

A component offers only the variants **both** its package and the design system declare. A session may be restricted to a **template set** (a story's chosen templates): the catalogue then lists only those, and a screen on any other template is refused with alternatives from the set.

## Design operations

An operation is plain data with an `op` discriminator naming one change.

| `op` | Does |
| --- | --- |
| `addScreen` | Adds a screen on a catalogue template and appends it to the screen set. |
| `removeScreen`, `moveScreen` | Removes (warning `linkDangling` for links to it) or reorders; the default storyline follows. |
| `insert` | Puts a component in an outlet of a screen; its id is generated unless given. |
| `set`, `remove`, `move` | Changes, deletes or relocates an element. |
| `link` | Makes an element navigate to another screen of the design. |
| `profile` | Changes `theme`, `colorScheme`, `accent`, `corners`, `font`, `rootTemplate` or `newScreenTemplate`. |
| `variations` | Offers up to three layouts of a screen on other templates, saying what did not fit; changes nothing until one is chosen. |
| `tweaks` | Offers up to five controls (a hue or a choice) bound to a profile key, an element property or a token. |
| `refuse` | Says no, with a code, a reason and alternatives. |

Template mode adds `addTemplate`, `addSlot`, `removeSlot` and `arrange`, and `insert`, `set`, `remove`, `move` and `link` then name a `template` instead of a `screen`. Prefer operations for every change to an existing screen: they are deterministic, checked and one revision each. Write a whole screen as text only to create it.

## Checks and refusals

| Check | Refusal code |
| --- | --- |
| The component, template, screen, element or outlet exists | `componentNotInCatalogue`, `templateNotInCatalogue`, `screenUnknown`, `elementUnknown`, `outletUnknown` |
| The outlet takes the component and has room | `outletDoesNotAccept`, `outletFull`, `overlayNeedsAction`, `shellComponent` |
| The property exists and takes the value | `propertyUnknown`, `valueNotAllowed`, `queryUnknown` |
| The variant is declared by the design system | `variantNotInDesignSystem` |
| No free styling | `freeStylingNotAllowed` |
| No value of its own | `offSystemValue` |
| Profile keys and values | `profileKeyUnknown`, `profileValueNotAllowed`, `noProfile` |
| The template set is respected | `templateNotInSet` |
| The engine accepts the Scene edit | `sceneRefused` |

A refusal carries `code`, `reason`, `alternatives` (the nearest accepted operations, and design-system proposals) and `diagnostics`. Alternatives are the outlets that do take the component, the variants it has, the queries that exist, the values a profile key takes, the templates of the set. Take an alternative when one fits the person's intent; otherwise tell them what was refused and why.

## Free styling is refused

A design says *which* component, *which* variant and *where*; the design system says what it looks like. These are refused, never worked around:

- style properties (`background`, `color`, `padding`, `className`, `style`, ...): `freeStylingNotAllowed`;
- a value of its own (a hex colour, `linear-gradient(...)`, markup, an `accent` the design system has no colour for): `offSystemValue`.

Never retry a refused change under another property name, as raw markup or as text in a label. "Make the button red" is answered with a variant the design system declares, or with a proposal for one.

## Proposals into the design-system draft

The canvas edits **screens only**. When a design needs something the design system lacks (a token, a variant, a word, a component), the agent does not edit the design system: a refusal carries, or the agent files, a design-system **proposal**, which the host routes into the product's design-system **draft**. The draft follows the product's review and activation; nothing reaches the screens until it is active. A tweak bound to a token is the same: it lowers to a proposal, not to a screen edit. Screen templates designed in template mode are saved into the draft the same way.

```json
{
  "applied": false,
  "code": "variantNotInDesignSystem",
  "reason": "The design system has no 'danger' variant of Button. Variants: primary, secondary.",
  "alternatives": [
    { "label": "Use primary", "operations": [{ "op": "set", "screen": "InvoiceDetails", "element": "pay", "prop": "variant", "value": "primary" }] },
    { "label": "Propose it to the design system", "proposal": { "kind": "variant", "component": "Button", "variant": "danger", "summary": "Add the danger variant of Button" } }
  ]
}
```

A proposal names the need and the reason and applies nothing. Say plainly that it is a proposal and that the screen keeps its current look until the draft is activated.

## Screens, free screens and the session

- **A new screen is a free screen.** It needs no slice and no event model. Its data needs are shapes ([free-screens.md](free-screens.md)); a slice adopts it later, in the model, with `uses screen` and its provisions, which is not part of the design turn.
- **Screens, links and storylines.** `link` sets a navigation destination on an element. The screen set keeps a default storyline that always lists every screen in order; write another storyline for an alternative path.
- **The session remembers, the model decides.** A session records revisions per screen (the text, the operations, the prompt, the author, the turn id, the design-system version), variation groups, tweak controls and comments. Only each screen's current revision is written into the model. A revert is a new revision, nothing is deleted.
- **Comments are addressed by element.** Read comments by the element's stable Screenplay `id` and answer them as operations on that element.
- **Variations and tweaks change nothing until chosen.** Offer them; do not apply one on the person's behalf.

## A worked turn

The person asks for a red "Pay" button on `InvoiceDetails`.

1. Read the design system's `Button` component: variants `primary` and `secondary`.
2. A `set` of a colour property is refused (`freeStylingNotAllowed`); a `set` of `variant` to `danger` is refused (`variantNotInDesignSystem`) with the alternatives above.
3. The alternative `primary` does not fit the intent, so apply nothing. Reply that the design system has no destructive variant, and offer to keep `primary` or to propose a `danger` variant to the design-system draft.
4. If they want the proposal, file it. The screen keeps `primary` until the draft is activated.

## Checklist

- [ ] Every component, template, token and variant came from a catalogue answer.
- [ ] Existing screens changed through operations, not rewritten; every outcome was read.
- [ ] No free styling, hex value or markup in any write; refusals were answered with an alternative or a proposal.
- [ ] A missing token, variant, word or component is a draft proposal, never a screen edit.
- [ ] New screens are free screens; adoption by a slice is left to the model, and a design-only screen is not reported as delivered.
