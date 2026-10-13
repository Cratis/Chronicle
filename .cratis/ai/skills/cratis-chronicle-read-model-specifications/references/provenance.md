<!-- cratis-ai-managed: skills/cratis-chronicle-read-model-specifications/references/provenance.md -->
# Provenance

Lineage of the #493 additions to `cratis-chronicle-read-model-specifications`. No TrogonStack material was adapted. Nebulit material is credited below.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/build-state-view/SKILL.md`: checklist: every field asserted, every event covered, empty-list case | `SKILL.md` Step 6 | idea; our own wording kept |
| `.claude/skills/build-state-view/SKILL.md`: one specification per contract specification, population on the first event, removal on the end event, assertions on the materialized state, optional storyline-derived read-model chain tests | `references/view-lifecycle-checks.md` | Adapted closely as population, update and removal checks over `ReadModelScenario<TReadModel>`; the partial-update and several-instance checks are ours. Not adopted: SQL insert, update and delete operation patterns, Flyway migrations, `PostgreSQLProjectionSpec` with Testcontainers, route creation and wiring; storyline-derived tests wait for storyline syntax (Screenplay#394), lifecycle spec families stand in |

The strict-subscription reconciliation is original to Cratis and verified against Chronicle v18.3.0 and v19.32.0.
