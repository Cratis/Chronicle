<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/reaction-forms.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Reaction trigger and effect forms

Excerpts from `complete-examples.md`: "Reaction on a declared trigger with `where`
and `invokes`" and "Reaction with `produces` and `invokes`".

```screenplay excerpt
slice Automation NotifyOnBuildFailure
  reaction NotifyOnFailure
    description "Tells the owning team when a watched build fails"
    when BuildFinished
      repository
      outcome
      invokes SendFailureNotice
        repository = repository
        outcome    = outcome
    where outcome == "failed"
```

| Form | Runs |
| --- | --- |
| `when <Name>` | on that event, declared trigger, or registered trigger |
| `every <n> seconds\|minutes\|hours\|days` | on that interval (`n` ≥ 1) |
| `at HH:mm` | every day at that time |
| `at HH:mm on <Weekday>` | every week on that day |
| `at HH:mm on day <n>` | every month on that day (`n` is 1–31) |

Time is strictly `HH:mm`. Weekday is the full English name. A bare name under a
trigger selects a value from the occurrence; it is not a new declaration. Taking
a value absent from the event/trigger shape is reported.

```screenplay excerpt
reaction Provisioner
  when InvitationAccepted
    workspaceId
    produces WorkspaceProvisioned
      for workspaceId
      workspaceId = workspaceId
    invokes SendWelcomeMail
      workspaceId = workspaceId
      template    = "welcome"
```

A `produces` without `for` appends to the source of the triggering event. Clock,
application and built-in triggers need an explicit destination. Producing requests
an append subject to constraints; invoking also runs command authorization,
validation and requirements and may reject. They are not interchangeable.

These are declarations, not implementations. A trigger can carry a `file` or
inline tagged fence beside its consequences; the model inventories role, owner
and content hash but does not execute that attachment. Stage mixed-effect
refusals and reference unsupported outcomes remain separate checks.

`where` is reaction-level, with only one occurrence allowed; combine conditions
with `and`/`or`. Its source grammar includes `contains` and `starts with`, but the
executable binder refuses those operators with PLAY0268. Use the literal property
rules for validation, not as a way around condition admission.
