---
title: "Issue labels"
description: "How Chronicle's issue labels are organised, which ones carry a meaning for automation, and why idea and investigate issues are never stale."
---

Chronicle's labels form a deliberate **area × quality-attribute** system. An issue is placed by the part of Chronicle it concerns (its *area*) and by the property of the system it affects (its *quality attribute*). There is no defect-type axis, no `bug` label: a defect is expressed as a quality attribute such as `reliability`, `consistency`, `performance` or `memory`, combined with the area where it happens.

Read the axes below before labelling an issue, and before deciding that a set of issues is stale.

## The axes

### Type and workflow

What kind of work the issue is, or where it stands.

| Label | When it applies |
| ----- | --------------- |
| `idea` | A suggestion that is not committed work. Long-lived by design, see [below](#idea-and-investigate-are-long-lived-by-design). |
| `investigate` | An open question that needs research before it becomes work. Long-lived by design. |
| `technical debt` | Work that improves the code without changing behavior. |
| `chore` | Routine upkeep. |
| `specs` | Missing or broken specs. |
| `documentation` | Documentation work. |
| `question` | A request for information. |
| `blocked` | Waiting on something outside the issue. |
| `wait for consolidation` | To be merged with other work before anyone starts. |
| `duplicate`, `invalid`, `wontfix` | The reasons an issue is closed without being done. |
| `good first issue`, `help wanted` | Invitations to contributors. |

### Quality attribute

Which property of the running system the issue concerns.

| Label | When it applies |
| ----- | --------------- |
| `reliability` | Staying up and keeping processing, such as a stalled observer or a lost connection. |
| `consistency` | Keeping the event store and the read models correct. |
| `performance` | Speed and throughput. |
| `memory` | Using more memory than necessary. |
| `maintainability` | Code that is hard to change safely. |
| `testability` | Code or behavior that is hard to test. |
| `observability` | Logging, metrics and tracing. |
| `security` | Authentication, authorization and protection of data. |
| `compliance` | Handling of personal and regulated data. |
| `devex` | The experience of developers using or contributing to Chronicle. |

### Area

The part of Chronicle the issue is in. An issue usually carries one area and one or more quality attributes.

| Label | Covers |
| ----- | ------ |
| `events`, `streams` | Event types, appending, event sequences and streams. |
| `observers`, `replay` | Reactors, reducers, observer state and replaying. |
| `projections`, `sinks`, `storage` | Building read models and where they are stored. |
| `jobs` | The job system. |
| `clustering` | Running several kernel nodes. |
| `serialization`, `custom keys` | Serialization and key handling. |
| `configuration` | Configuration on the client or the server. |
| `api`, `client`, `workbench` | The public API, the client libraries and the Workbench. |
| `recommendations` | The recommendation system. |
| `upgrade` | Upgrading between versions. |
| `build`, `branching` | Build, release and branch workflow. |
| `Orleans` | The Orleans runtime the kernel is built on. |
| `.NET`, `javascript`, `dependencies` | Dependency updates, applied by the dependency bots to pull requests. |

### Release intent

`major`, `minor`, `patch` and `no-release` belong to **pull requests**, not issues. The pull request check requires exactly one of them on every pull request into `main`; the rules are kept in the organization's shared [release-intent workflow](https://github.com/Cratis/Workflows/blob/main/.github/workflows/verify-release-intent.yml) and not repeated here. On an issue, the signal that something breaks consumers is the separate `breaking change` label.

## Labels that automation reads

Changing what these labels mean changes what CI does.

| Label | Read by | Effect |
| ----- | ------- | ------ |
| `no-release` | `publish.yml` and the release-intent check | The pull request publishes nothing. |
| `specs-removal-intended` | `assert-spec-deletions-covered.py` | Lets a pull request delete specs on purpose. |
| `run-integration` | `requested-integration.yml` | Runs the Docker integration suite on the pull request. |

## `idea` and `investigate` are long-lived by design

An issue labelled `idea` or `investigate` is a reminder or an open question, and it is expected to stay open for a long time. Its age says nothing about whether it still matters.

Such issues are exempt from every staleness rule, whether applied by a person or by an agent: do not close, relabel or de-prioritise them because nobody has touched them. An automated sweep that treated all old issues alike is why this is written down.

The weekly issue analysis reports issues without an update for two years under "Potentially Obsolete", and that report does not look at labels yet. An `idea` or `investigate` issue listed there is not a candidate for closing.

## Closing requires evidence

An issue is closed when one of these is true:

- The behavior has shipped, and you can point at the change.
- The question has been answered.
- It duplicates another issue, and you can point at that issue.
- A person has decided it is out of scope, and said so on the issue.

"Nobody has touched it" is not evidence, on its own or in a count of how long it has been.

## `stale-review` is temporary

`stale-review` marks the issues that were reopened after the 2026-08-24 automated stale sweep and still await a human look. Remove it from an issue when you have re-triaged it. The label itself is deleted when no open issue carries it any more.
