---
name: cratis-engineering-regression-cluster
description: Stop fixing instances when one surface keeps breaking - name the failure class, add the gate that owns it, prove the gate fails, then fix the instance. Use when a third bug, regression, revert or review finding lands on the same screen, endpoint, command, workflow or module, when the same kind of fix keeps being reapplied, or when asked why something keeps breaking. Not for a first or isolated bug (fix it with a regression specification), for proving a consolidation dropped nothing (use cratis-engineering-consolidation-guard), or for a boundary that reports success after failing (use cratis-engineering-effect-boundaries).
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-engineering-regression-cluster/SKILL.md -->

# The third issue on one surface names a failure class

Each bug fixed alone adds one specification for one symptom. When the same
**surface** (one screen, endpoint, command, query, workflow, module or rule
area) breaks a third time, the instances share a cause that no specification
owns yet, and the fourth instance is already on its way. Stop fixing instances:
name the **failure class**, give it an **owning gate**, then fix the instance.

## Count first

Before fixing an issue, count the earlier ones on its surface:

```bash
gh issue list --state all --search "<surface> in:title,body" --limit 50
git log --oneline -- <paths of the surface>
```

Count bugs, regressions, reverts and review findings, open or closed. Fewer
than three: fix the instance with a regression specification. Three or more:
follow the steps below before writing the fix.

## Steps

1. **Name the failure class** in one sentence that states the property being
   violated, not the symptom. Each earlier issue must be an instance of it, and
   a reader must be able to recognize the next one by it. "Owner filter shows
   archived items" is a symptom; "list filters are not exercised through the
   rendered list by any test" is a class.
2. **Choose the owning gate by enforcement kind**, preferring the earliest that
   can catch the class: an analyzer (compile time), a lint rule, a CI check or
   verification script, or one specification suite that covers every member of
   the class (every filter, every endpoint), not one specification per bug. A
   class that only judgment can catch gets a review checklist entry, and the
   choice is stated as judgment-shaped; see the enforcement rule in
   [authoring-skills.md](../../rules/authoring-skills.md).
3. **Prove the gate fails** on the current broken instance, or on the earlier
   instances with their fixes reverted, before fixing anything. A gate that
   passes on the broken code owns nothing; follow
   [guards-and-fuses.md](../../rules/guards-and-fuses.md) so it cannot pass
   vacuously.
4. **Register the class.** When the repository keeps a failure-class table or
   gate inventory, add one row: class, owning gate, where it runs. Otherwise
   put the class name in the gate's failure message and the specification or
   check name, so the next failure names its class.
5. **Fix the instance**; the gate turns green. Run it with the repository's
   normal gates, and make it a required CI check when it lives in CI.
6. **Close the loop.** Link the earlier issues to the class in the pull request
   comment, and leave a prose rule only for the judgment-shaped part.

## Report

State the surface and the issues counted, the class sentence, the gate and
where it runs, that it failed before the fix and passes after, and anything
left judgment-shaped.

## Pitfalls

- **A fourth instance specification.** Copying the third fix's specification
  for the new symptom covers the symptom, not the class.
- **A prose rule instead of a gate.** Rules nobody enforces were already in
  place for the earlier instances; text costs context and catches nothing.
- **A class named after the symptom.** If the next instance would not be
  recognized by the sentence, the class is still too narrow.
- **A gate that never ran red.** Without the failing run, nothing shows it can
  catch the class.
- **A loose surface.** Three unrelated bugs in one large module are not a
  cluster; narrow the surface until the issues share a cause.
