<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/provenance.md -->
# Provenance

| Item | Source | Licence | Kind | Where in this skill |
|---|---|---|---|---|
| Conditional ownership interview (team structure, autonomy, external integrations, skip rule, follow-up triggers) | TrogonStack/agentskills@7b249d3:plugins/trogonstack-eventmodeling/skills/eventmodeling-applying-conways-law/SKILL.md | MIT, Copyright (c) 2025 Straw Hat, LLC | ADAPT (reworded for Screenplay; the "team = system" absolutes dropped) | SKILL.md "Interview phase", `boundaries.md` |
| Interface and processor inventory per boundary, communication trace, system-organization output shape, quality checklist | same file | MIT | ADAPT | `boundaries.md` "Ownership and interface inventory", "Boundary record" |
| Event-by-event stream identity check, decision per identity, "design first, snapshot second" | TrogonStack/agentskills@7b249d3:plugins/trogonstack-eventmodeling/skills/eventmodeling-optimizing-stream-design/SKILL.md and its patterns reference | MIT | ADAPT (arbitrary length thresholds and mandatory archival splits dropped) | `stream-patterns.md` |
| Growth-estimate interview and the order "redesign, then read model, then measure, then snapshot" | same skill | MIT | ADAPT | SKILL.md "Interview phase", `stream-patterns.md` "Growth and snapshots" |
| Invariant table, state-dependent rule recording, race/retry/duplicate scenarios, evolution classes, Chronicle boundaries | Cratis corpus drafts | MIT | ORIGINAL | `consistency-and-concurrency.md`, `evolution.md`, `chronicle-boundaries.md` |
| Worked examples (courses and enrolment, memberships, marina berths) | written for this skill | MIT | ORIGINAL | `streams-example.md`, `evolution-example.md`, `boundaries.md` |

Event Modeling practice is by Adam Dymitruk and Martin Dilger, cited as practice; no text taken.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-optimizing-stream-design/SKILL.md`: golden rule, identity-then-every-event review, classification table | `SKILL.md` Procedure 1, `references/stream-patterns.md` "Identity and event-membership review", "Interpret the evidence" | adapted closely; "long history is fine" kept, translated to `identifier`/`for` |
| `.claude/skills/eventmodeling-optimizing-stream-design`, patterns reference: decision tree, red flags 2-4, composite and collection and log patterns | `references/stream-patterns.md` "Boundary decision tree", "Red flags", "Boundary patterns by domain" | adapted closely; red flag 1 ("no natural end") dropped because it contradicts long-lived identities; event-granularity tip dropped (generic update events are a defect here) |
| `.claude/skills/eventmodeling-optimizing-stream-design`, domain-patterns reference: per-domain identity entries | `references/stream-patterns.md` "Boundary patterns by domain" | translated to invoicing, marina, memberships, enrolment and hire; lifetimes and archival splits dropped |
| `.claude/skills/eventmodeling-applying-conways-law/SKILL.md`: interview questions with follow-up triggers, six-step ownership workflow, quality checklist | `references/boundaries.md` "Ownership interview", "Ownership and interface inventory", "Responsibility matrix and ownership timeline", "Boundary checklist" | adapted closely; "team = system" absolutes deliberately not adopted, kept "not by law" |
| `.claude/skills/eventmodeling-applying-conways-law`, examples reference: responsibility matrix, swimlane timeline, inter-system communication | `references/boundaries.md` "Responsibility matrix and ownership timeline", "Worked walkthrough" | translated to marina and invoicing (see the next row for the indirect-producer attribution) |
| `.claude/skills/eventmodeling-core-rules/SKILL.md`: immutability, calculated-event anti-pattern, causality, open question versus decided failure, "left chair" fan-out, offline-first | `references/stream-patterns.md` "Core rules that bear on stream design", `SKILL.md` modeling defaults | adapted closely, translated to Screenplay constructs; board lanes, linked copies and role catalog not adopted here (other skills own them) |
| `.claude/skills/eventmodeling-applying-conways-law/SKILL.md`, examples reference: team and system boundary as one, indirect-producer attribution | `references/boundaries.md` "Ownership by design (optional lens)" | Adapted closely as an optional lens with the "team = system" claim kept as a hypothesis to test; indirect-producer attribution adopted for public events, with ownership staying with the deciding boundary |
