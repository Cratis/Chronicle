<!-- cratis-ai-managed: skills/cratis-application-slice-specifications/references/provenance.md -->
# Provenance

Lineage of the additions to `cratis-application-slice-specifications`. No TrogonStack material was adapted. Nebulit material is credited below.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/build-automation/SKILL.md`: checklist question "what happens if the command fires twice" | `SKILL.md` Step 3 item 5 | idea; our own wording kept |
| `.claude/skills/build-automation/SKILL.md`: one error specification per rule; build from the slice definition first | `SKILL.md` Step 0 and Step 6 | idea; our own wording kept |

## Eventmodelers Build-Kits

Source: https://github.com/Nebulit-GmbH/Eventmodelers-Build-Kits, by Martin Dilger and Nebulit GmbH
(https://nebulit.de), used with their explicit consent.

| Source skill | Used in | How |
|---|---|---|
| axon/kurrent `build-state-view` query tests | `query-and-decision-scenarios.md` | copied and translated: isolate query results by key/filter/caller scope and assert complete items where fully contracted; provider integration remains separate |

## Method lineage

| Idea | Used in | Credit |
| --- | --- | --- |
| Scenario decision state and essential values become setup and assertions, while background notes stay context | `SKILL.md`, Reading a `.play` specification | Martin Dilger / Nebulit GmbH, Eventmodelers build kits (`place-element`, `examples`, `classic build-state-change`, `classic build-state-view`), used with their explicit consent; idea, our own wording |
