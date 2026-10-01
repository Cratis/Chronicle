---
applyTo: "**/*"
---

## Issue label conventions

Chronicle's labels are an **area × quality-attribute** system with no defect-type axis. A defect is a quality attribute (`reliability`, `consistency`, `performance`, `memory`) combined with the area it occurs in. The full taxonomy is in [Issue labels](Documentation/contributing/issue-labels.md).

- **Axes.** Type and workflow (`idea`, `investigate`, `technical debt`, `chore`, `specs`, ...), quality attributes, areas (`observers`, `projections`, `events`, ...), and release intent (`major`, `minor`, `patch`, `no-release`), which belongs to pull requests only.
- **`idea` and `investigate` are long-lived by design.** They are exempt from every staleness heuristic, for people and agents alike. Never close, relabel or de-prioritise them because of age. The weekly issue analysis does not honor this yet, so a listing under "Potentially Obsolete" is no reason to act on them.
- **Closing requires evidence**, never age alone: the behavior shipped, the question was answered, a confirmed duplicate, or a person decided it is out of scope.
- **`stale-review` is temporary.** Remove it from an issue once it has been re-triaged by a person.
