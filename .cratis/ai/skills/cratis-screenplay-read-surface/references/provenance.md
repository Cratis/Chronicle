<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/provenance.md -->
# Provenance

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger
and Nebulit GmbH. The notice is in this skill's `LICENSE`.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-storyboarding-events/SKILL.md`, "Common Patterns": input screen, status screen, error state | `references/screen-patterns.md` | Adapted closely and mapped to `screen`, `data`, `action` and `specification` (`then error`); one screen per column, lanes, actor perspective rows and mandatory rendering not adopted; the example is compiled with Screenplay 4.139.0 |

Not adopted: HTML screen reconstruction and wireframe sketching (`html-screen`, `storyboard-screen`), which are board
mechanics; Screenplay screens are declarative and Stage renders them.

## Other lineage

- Screenplay language documentation and the released tool, pinned in `cratis-screenplay-toolchain`
  `references/versions.md`: the `readmodel`, `query` and `screen` constructs.
- Event Modeling (Adam Dymitruk) and Martin Dilger, *Understanding Eventsourcing*: the screen as the point where
  a person reads a view and decides.
