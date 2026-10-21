<!-- cratis-ai-managed: skills/cratis-screenplay-discovery/references/event-naming.md -->
# Naming events and filtering non-events

## Contents

- Naming
- Not an event (set aside, often a view or a question)
- Repairs (illustrative, marina domain)
- Concrete over generic
- Granularity checks (questions, not laws)

## Naming

- **Past tense, business verb.** The name says what became true: `BerthAssigned`,
  `FeeWaived`, `InspectionFailed`.
- **Two to four words**, subject first when it helps: `BoatArrived`, `BerthRequestDeclined`.
- **Specific over generic.** A generic verb (`Changed`, `Updated`, `Processed`, `Handled`,
  `Created`) hides which business decision was taken. Ask what kind of change it was.
- **The business's words**, not the software's: no `Record`, `Row`, `Entity`, `Dto`, `Request`
  (unless a request is itself a business act, as in `BerthRequested`).
- **Self-describing.** Someone reading the event list alone should understand the story.
- **Properties** are camelCase domain names (`berthNumber`, not `id2` or `berth_no`). The
  identity of the thing the event happened to is not a property; it comes from `for` later.

## Not an event (set aside, often a view or a question)

| Kind | Looks like | Why it is not a fact | What to do instead |
|---|---|---|---|
| Looking | someone opened, viewed, searched, filtered | nothing changed in the business | a read model need; note it for `cratis-screenplay-slice-design` |
| Technical | sent a request, saved a row, synced, retried, logged in (session) | describes the software, not the business | ask what business outcome the step served |
| Generic edit | `XUpdated`, `XChanged`, `XEdited`, `XSaved` | hides the decision | split into the specific changes people actually make |
| Running figure | balance, total, occupancy rate, count so far | recomputed from facts, changes all the time | a view, unless it is a decision (below) |
| Not yet | might happen, is expected, should be | speculative; nothing became true | model the fact when it happens; deadlines are reached facts |
| Still going | is pending, is waiting, is in progress | a state between facts, not a fact | name the fact that started it (`BerthRequested`) |
| Secret or bearer value | token, magic link, signed URL, password in a payload | a credential; the event log is permanent | record a keyed hash or opaque reference |
| Aggregate plus fan-out | `RequestsSent` with a list *and* one `RequestSentToPartner` each | the same fact twice; they can disagree | keep one shape, chosen by how consumers read it |
| Attempted | tried and was refused | a rejected command leaves no fact | a rejection scenario, unless the business must remember the refusal |

**Exceptions that are facts:**

- A calculated result the business decided on and must be able to show later (a quoted price,
  a fee fixed for the season, a risk assessment, a grade) is a fact: record it when decided.
- A refusal the business must remember (a declined application, a failed inspection) is a
  decision with an outcome and is a fact; a refused click is not.
- A deadline passing is a fact when the business acts on it or must prove it was noticed
  (`PaymentDeadlinePassed`).

## Repairs (illustrative, marina domain)

| Candidate | Problem | Better |
|---|---|---|
| `BerthUpdated` | generic edit | `BerthReassigned` or `BerthLengthCorrected`, whichever happened |
| `HarbourmasterViewedWaitingList` | looking | set aside; a waiting-list view |
| `OccupancyRecalculated` | running figure | a view; no event |
| `SeasonFeeCalculated` | maybe a decision | `SeasonFeeSet` if the fee is fixed and must be shown later |
| `BoatWillLeave` | not yet | `BoatDeparted` when it happens; `DepartureNoticeGiven` if notice is a business act |
| `ArrivalPending` | still going | the starting fact: `BerthAssigned` |
| `SyncWithHarbourAuthorityDone` | technical | what the authority told us: `MooringPermitGranted` |
| `AssignBerthFailed` | attempted | a rejection scenario on `AssignBerth` |

## Concrete over generic
Reading the model, a person must understand how this business works. Model one real flow with
real examples and real values, not a generic structure that could hold every case. This holds for
software that is generic by design (a form builder, a workflow engine, a case or record system):
model one real case it handles, not its machinery. Brownfield generic engines are read the same
way (`cratis-screenplay-legacy-extraction` `references/signal-to-intent.md`).

- Wrong: `SubmitForm` then `FormSubmitted` with `formId` and `content`, or with `fields`,
  `category` and `entries`. It says nothing about the business.
- Right: `ReportHullDamage` then `HullDamageReported` with berth `A-12` and the damage "Scratched
  starboard bow, about 40 cm".

When the generic platform is itself the product being built, its own facts
(`FormTemplatePublished`) are the business. Ask which of the two you are modeling. The example uses
no personal data on purpose: a health-record example would need `pii` concepts and a `purpose`
(command-surface `references/compliance.md`).

```screenplay
domain Harbour.Moorings

concept BerthNumber : String
concept DamageDescription : String

module Moorings
  feature HullInspections
    description "One real flow: a harbourmaster reports damage found on a moored boat"
    slice StateChange ReportHullDamage
      command ReportHullDamage
        berthNumber BerthNumber identifier
        damage      DamageDescription
        produces event HullDamageReported
          damage DamageDescription = damage
      specification ReportingScratchedBow
        when ReportHullDamage
          berthNumber = "A-12"
          damage      = "Scratched starboard bow, about 40 cm"
        then HullDamageReported
          for "A-12"
          damage = "Scratched starboard bow, about 40 cm"
```

## Granularity checks (questions, not laws)

- Would the business ever react to one part of this without the other? If yes, consider two
  events.
- Optional event properties: the default is none. Chronicle warns on nullable event
  properties (CHR0012) and a specification cannot state `null` for an event value (PLAY0350).
  Make the value required, or record the second situation as its own event. Keep `optional`
  only for a detail the business genuinely may not have, with the reason in event `documentation`.
- Does the event restate the creation event's fields (a generic edit in disguise)? Ask which
  business change it records; if none, name the specific correction or drop it.
- Never put personal data in the event-source id; one data subject per event.
- Store a deadline, not clock-relative state (`IsOverdue`).
- Does the event carry data about two different people? Consider one event per person.
