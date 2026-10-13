<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/phase-report-inventories.md -->
# Phase report inventories

Two artifacts, never conflated (`handoff-template.md` section 2):
- **Phase report**: exhaustive, per-element, evidence-bearing. Goes in the final message when the
  brief says so; otherwise in a task-scoped file under `.ai-work/screenplay/<model-slug>/`
  named in the packet, never inside the model root.
- **Handoff packet**: about 40 lines, five verdict lines, a pointer to the report.

Per phase, the report carries one line per element, not only problems; every omission is a stated
exemption or a gap.

| Phase skill | Report inventory |
|---|---|
| `cratis-screenplay-discovery` | modules, features, personas with purpose; events per workflow in story order with terminal states; rules register and denial candidates; sweep answers (fact / set aside / question / "n/a, because"); open questions |
| `cratis-screenplay-slice-design` | per command: origin(s); success event(s); each refusal with layer and message; the caller fixture denied when gated. Per screen: its view or the creation exemption. Per read model: consumer(s) and a reason per contributing event. Per slice: events consumed and producing slice (orientation, not build order). Then lineage gaps, target-enforced rules, open questions |
| `cratis-screenplay-streams-and-consistency` | per event source: identity, invariants and where each is enforced, races, retries and dedup scope, evolution class per changed event, identity notes |
| `cratis-screenplay-automations-and-translations` | per reaction or capture: trigger, produces vs invokes, trusted actor, termination, dedup, external effect and what once-only does not prove |
| `cratis-screenplay-scenario-coverage` | coverage matrix (command, query, view by scenario type: spec, or a domain-reason n/a), shared example data, spec count by type |
| `cratis-screenplay-model-review` | findings with tier and evidence, the completeness walk, business questions |
| `cratis-screenplay-legacy-extraction` | evidence table rows with locators (file names exactly as given), keep / change / remove, loss report, expert questions |
| `cratis-screenplay-render-and-gap-fill` | V5 sub-results, generated spec-class mapping, edit requests, fallback entries with blocking codes, authored-UI omissions |

`Changed declarations` and `Open questions` in the packet summarize; the packet names the report
location (`Report: final message | <path>`). A model too large for the packet's 40 lines is exactly
the case where the report carries the tables.
