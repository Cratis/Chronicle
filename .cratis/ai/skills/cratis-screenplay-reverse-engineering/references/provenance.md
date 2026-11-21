<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/provenance.md -->
# Provenance

| Content | Source | License | How used |
|---|---|---|---|
| Evidence rows that carry explicit unknowns, source kinds and locators (`references/evidence-format.md`); static before dynamic, per-kind techniques, tracing a feature across IPC, network and storage, and treating process capture as unisolated (`references/executable-intake.md`); fidelity verification ideas: behavior ledger with case kinds, undecided never passing, authority matching the claim, negative controls, read-only probes of owned originals, withheld effects, run provenance (`references/fidelity-check.md`) | morluto/rea (https://github.com/morluto/rea) | MIT | Ideas only, restated in our own wording and fields; no upstream text or schema is reused |
| The bounded, state-aware UI walk | Martin Dilger and Nebulit GmbH, agentic-engineer and Eventmodelers Build-Kits, used with their explicit consent; adapted in `cratis-screenplay-legacy-extraction` | see that skill's `LICENSE` and provenance | Referenced, not copied: this skill points to `ui-observation.md` and `signal-to-intent.md` there |
| Inverse event modeling: following one flow from entry point to persisted effect; a watermark naming the state a model describes; process drift rather than architecture drift; flow-sized review units; a recorded reason per mapping (`references/process-drift.md`, `references/evidence-format.md`, `references/scoping.md`) | Martin Dilger, "Inverse Event Modeling" | idea only | Credited as ideas; no text is reused |
| Workflow, safety rules, synthesis and fidelity references | Cratis/AI corpus | MIT (Cratis) | Own work |

The method spine (L0-L4, evidence ladder, hosted-model disclosure rule) is the legacy extraction
skill's; see its `references/provenance.md` for the full table of adapted sources.
