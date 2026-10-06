<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/provenance.md -->
# Provenance

## Adapted closely (Martin Dilger and Nebulit GmbH, with agreement)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de). The repository carries no licence file; this material
is adapted with the agreement of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/build-automation/SKILL.md`: reactor checklist: output fields traced to a stated source, skip conditions stated in the contract, repeated-input cases | `SKILL.md` "Specifications" and "Verify" | idea; our own wording kept |

The `WithStrictEventSubscription()` scope statement and the scenario sequence-number behavior are verified against Chronicle `v19.32.0` source (`Source/Clients/Testing/ReadModels/ReadModelScenario.cs`, `Source/Clients/Testing/Reactors/ReactorScenario.cs`).
