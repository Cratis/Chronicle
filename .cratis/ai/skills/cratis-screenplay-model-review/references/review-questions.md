<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/review-questions.md -->
# Review questions

Questions to ask while reviewing, by subject, and what separates a fact from a view in
different domains. Adapted from the "Questions to Ask During Validation" and "Checklist
Questions by Domain" sections of TrogonStack `eventmodeling-validating-event-models-checklist`
(see `provenance.md`), mapped to Screenplay constructs. Use them to find the evidence a check
needs; a "no" is a finding only when it has a business consequence.

## For each kind of event source
1. Do all events appended `for` this identity describe one business thing (checklist B2, B4)?
2. Could these events occur in any order, or is the sequence important? If it matters, which
   constraint or `then error` specification pins it (F2, T3)?
3. Is every event a fact that actually happened, in the words the business uses (C1, C2)?

## For each command
1. Who can reach it, through which screen action, form, reaction or capture, and which policy
   lets them (G1, G7)?
2. Which fact does it produce, or which refusal does it document, and where is the refusal
   pinned (S5, T3)?
3. Which stored state does its rule need, read through `reads`, and is the rule marked as not
   enforced when the model cannot enforce it (E3)?
4. Does it ask for the whole entity again, or for one business change (S3)?

## For each read model
1. Which projection or reducer builds it, and is it the only builder (D7, H3)?
2. Which screen, reaction or caller reads it, and what decision does the reader make from each
   field (A6)?
3. Could its figures change because a rule changed, without rewriting stored facts (D3)?
4. Does one query shape serve one question (H1)?

## For each Automation or Translate slice
1. Which pending-work view says what is outstanding, and which facts open and close it (F8, F12)?
2. Is it opened only by our own facts, or does an outside fact reach a decision directly (F9)?
3. Does the step after a translation add a distinct decision, obligation or effect, or does it only restate the translated fact (F11)?
4. Would this work exist if the whole process ran on paper? A step that exists only because of
   the system (a cache refresh, a loading state, a session check) is not a business step (C2).

## For each slice and the model as a whole
1. Does each slice depend on other slices only through event contracts, `invokes` and explicitly
   recorded cross-slice `reads` of another slice's view (F7)? A `reads` is unprotected (a
   stored-state rule over it stays not enforced) and couples the slice to that view's interface:
   check that it is recorded, not implied.
2. Could two people work on two slices at the same time without reading each other's
   declarations, given only the events' fields (A8)?
3. Is every shared artifact an event contract or a concept (B6)?
4. Does a cycle exist anywhere (F6)?

## Facts versus views, by domain
The principle is the same everywhere: immutable facts are events, calculated results are read
models; a decided figure that matters later is a fact.

| Domain | Facts (events) | Not events (views or derived) |
|---|---|---|
| Marina berths | `BerthRegistered`, `BerthAssigned`, `BoatArrived`, `BoatDeparted`, `SeasonFeeAssessed` | berth occupancy, free berths by length, days occupied this season |
| Invoicing | `InvoiceRegistered`, `InvoiceSent`, `PaymentReceived`, `InvoiceWrittenOff` | outstanding balance, overdue state (compare the stored due date at the edge), ageing buckets |
| Memberships | `MemberJoined`, `MembershipRenewed`, `MembershipLapsed`, `MembershipUpgraded` | active member count, renewal rate, time as a member |
| Course enrolment | `StudentEnrolled`, `PlaceOffered`, `PlaceDeclined`, `EnrolmentWithdrawn` | places left, waiting-list position, completion percentage |
| Equipment hire | `HireReserved`, `ItemCollected`, `ItemReturned`, `DamageAssessed` | availability calendar, utilisation, hire days to date |

`DamageAssessed` and `SeasonFeeAssessed` are decided figures: the business decided them at a
moment, and they are recorded. A running total of fees is not.

## Patterns to recognise while reading
| You see | Ask | If the answer is "no" |
|---|---|---|
| `TotalComputed`, `BalanceUpdated` | Is this immutable and caused by a person or system action? | It is a view; move it to a projection |
| One wide view used by several commands' `require` lines | Could two of those commands be changed by different people without touching each other? | Give each rule its own `reads` (anti-patterns: shared decision state) |
| A flag a command writes | Is it only needed while deciding? | Derive it from facts; do not store it |
| A read-model field with no mapping | Where does it come from? | An event or a mapping is missing, or the field goes (A1) |
