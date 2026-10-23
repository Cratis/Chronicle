<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/vocabulary.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# The specification vocabulary

This reference holds the given/when/then construct table and the enumeration and DateTime literal rules.

## The vocabulary

| Construct | Meaning |
| --- | --- |
| `given <EventType>` | prior state, established by replaying events before the action |
| `given readmodel <ReadModelType>` | prior read-model state, established directly — a **complete** instance including its identifier |
| `given caller` / `given caller as <Persona>` | explicit caller (`authenticated`, roles, claims; empty means anonymous), or an authenticated synthesized persona witness |
| `when <CommandType>` | run a command |
| `when append <EventType>` | append one event: constraints and projections run, the command does not; reactions run only from ESM v6, where `then` lists what followed |
| `when query <Query>` | perform a query with the argument values beneath it |
| `then result [exactly]` | one expected row of the performed query; repeat for several |
| `then no result` | the performed query returns nothing |
| `given clock "<instant>"` | the ISO 8601 instant the scenario happens at |
| `when clock "<instant>"` | the clock reaches an instant; scheduled reactions that are due run |
| `when trigger <Trigger>` | an application trigger fires with the values beneath it |
| `given capture` / `when capture <Capture>` | earlier/current records for non-event sources; for `source events` use typed `given <Event>` / `when append <Event>` (PLAY0701) |
| `generated <name> = <value>` | beneath `when <Command>`: the fixture for a generated property other than the identifier (ESM v7); `for` beneath the `when` supplies the generated identifier |
| `then returns [<value>]` | the command's response (ESM v7): `then returns "<uuid>"` for a scalar, `then returns` with named `field = value` lines for a subset of a record |
| `then <EventType>` | an expected new event |
| `then events in any order` | compare the new events without regard to order |
| `then readmodel <ReadModelType> [exactly]` | read-model state afterwards — must state the identifier |
| `then no readmodel <ReadModelType> for <key>` | exactly that keyed instance is absent; the key is concrete and typed |
| `then query <Query> [exactly]` | query results for explicit `arguments` (none for an unkeyed list); one `result` per row, each stating the identifier; none means empty |
| `file <path>` | optional, once: the repository-relative file realizing the specification |
| `then error "<message>"` | a validation or constraint rejection, for that reason; a localized rule is pinned by its quoted key, `then error "$strings.<key>"` |
| `then error` | a validation or constraint rejection, reason unnamed |
| `then denied` | an authorization denial (`Unauthorized`) |
| `for <value>` | the event source of a `given`/`then` event, or of the `when` command or appended event |
| `<property> = <value>` | a literal, `null`, or a one-line JSON-shaped object or list: `lines = [{"sku":"A-1","quantity":2}]` |

## Composite-keyed read models

Requires Screenplay 4.204.0 (ESM v27; `screenplay --version`); older binders refuse a
read model with two or more `key` marks, and every specification over it, with PLAY0268.

- `given readmodel` and `then readmodel` state every key part (missing part PLAY0351,
  repeated part PLAY0519).
- `then no readmodel <Type> for {"part":"value",...}` names every part (missing PLAY0661,
  unknown PLAY0662, repeated member PLAY0294).
- `when query` or `then query … arguments` supplies every `by` part of a single-instance
  query, or the equality filters of a list query (missing, unknown or repeated PLAY0468).
- A single-instance `then result` may omit parts the arguments supply; list rows state
  every part. A stated part that disagrees with the argument fails to match at run time.
- Parts match one by one in any order; Uuid parts compare as ordinal text.

The complete model with these specifications is in read-surface `references/named-keys.md`.

## Parse and binding diagnostics

A command or query authorized only by an anonymous policy may omit `given caller`;
every other authorized specification needs one (PLAY0389). See
[anonymous policy](../../cratis-screenplay-command-surface/references/policy-persona-seed.md#anonymous-policy).

PLAY0358 is a syntax error; PLAY0388 also occurs at parse time for extra text
after `then denied`. PLAY0350, PLAY0352, PLAY0388 for mixed outcomes, PLAY0389 and
the one-rejection rule PLAY0273 occur only at binding. Source validation alone
will not report them: check executable diagnostics as well. Clock instants need
an ISO 8601 offset or `Z`, for example `"2026-10-05T08:00:00Z"` (PLAY0461).

## Literal spellings

An enumeration value binds whether it is written bare
(`status = sent`), qualified (`status = InvoiceStatus.sent`) or quoted, and a
`DateTime` value binds as a plain ISO 8601 instant (`"2026-10-05T08:00:00Z"`).
An older compiler binds only the quoted member and the full round-trip instant
(`"2026-10-05T08:00:00.0000000Z"`) although it accepts all forms - quote members when a
model must bind on older versions.
