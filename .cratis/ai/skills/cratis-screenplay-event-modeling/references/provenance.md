<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/provenance.md -->
# Provenance

## Method lineage
- Event Modeling: Adam Dymitruk (eventmodeling.org); Martin Dilger, *Understanding Eventsourcing*.
- The two-phase process, nine steps, four patterns and GWT discipline follow
  [jwilger/agent-skills `event-modeling`](https://github.com/jwilger/agent-skills/tree/main/skills/event-modeling),
  as noted in `SKILL.md`; Cratis divergences are stated there.
- Screenplay vocabulary and the model-first rule come from the Cratis Screenplay documentation.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-orchestrating-event-modeling/SKILL.md`: "Do not cut corners to save tokens or effort" | `SKILL.md` paragraph "Do not cut corners to save tokens or effort" (short form; full text in the lifecycle skill) | Adapted closely; translated to Screenplay |
| `.claude/skills/eventmodeling-interview-protocol/SKILL.md`: when to ask, unless told not to ask | `SKILL.md` paragraph "Ask only what the step needs" (short form; full text in the lifecycle skill) | Adapted closely; recording moved to STATE.md |

### Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source skill | Used in | How |
|---|---|---|
| `eventmodeling-information-completeness` | `SKILL.md` *The prime directive* | adapted closely: every value starts at one specific place and is carried by name; an unknown origin is a gap, never a default |
| `eventmodeling-identifying-inputs` | `cratis-chronicle-event-modeling` `SKILL.md` *Decide before implementing* | adapted closely: a rule's decision values classified as input, state or context; context supplied by the pipeline or a time source, never read inside the decision |
| `eventmodeling-storyboarding-events` ("Mandatory Field Definitions") | `references/nine-steps.md` step 4 | copied and translated: a field is entered (becomes a command input) or shown (names its read model); a shown field never comes from the command the screen submits |
| `eventmodeling-orchestrating-event-modeling` ("Place in narrative order, never type by type") | `references/nine-steps.md` step 9; full text in `cratis-screenplay-slice-design` `references/slicing.md` | copied and translated: slices are listed by type to count and review, and declared in story order |

## Method lineage in SKILL.md

> **Method lineage.** The two-phase process, the nine steps, the four patterns and
> the GWT discipline follow **Event Modeling** (Adam Dymitruk; Martin Dilger,
> *Understanding Eventsourcing*), as structured in
> [jwilger/agent-skills `event-modeling`](https://github.com/jwilger/agent-skills/tree/main/skills/event-modeling).
> Screenplay adopts that vocabulary directly: *"Slices are the atom - everything
> lives inside a typed slice aligned with Event Modeling's vocabulary."*
> Where Cratis deliberately diverges, this skill says so.

The nine-step examples compile with the standalone compiler and their specifications run
in the reference runner (compilation does not establish reference execution).

Sources: Screenplay `Documentation/screenplay/{slices,commands,folders,printing,interactions,specifications}.md` and
`projections/keys.md`; the inline-contract and event-context changes follow `commands.md` and `events.md`.
