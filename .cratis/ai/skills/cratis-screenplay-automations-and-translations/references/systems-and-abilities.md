<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/systems-and-abilities.md -->
# Systems and abilities

## Contents

- Code computes, abilities touch the world
- Declare a system
- Effect classes
- Inputs, returns and outcomes
- Answers that arrive later
- Implementation
- The one `invokes` verb
- Invoke an ability
- Realize a system with an extension
- What is admitted and what is not

## Code computes, abilities touch the world

A model reaches something outside itself (a carrier, a ledger, a chat tool) through a `system` with `ability` declarations, not through an invented command or a free-text effect. Pure computation stays in code; anything with an external effect is an ability whose retry behavior, returned values and refusals are declared and therefore checkable. Record what an ability returned as a fact only through an event the model `produces`; an ability's result is never a fact by itself.

## Declare a system

```screenplay
concept ParcelId : Uuid
concept TrackingNumber : String

type Address
  street String
  postalCode String

system Carrier
  description "The parcel carrier's booking service"
  ability BookPickup idempotent
    description "Books a pickup for one parcel"
    parcelId ParcelId
    address Address
    returns
      trackingNumber TrackingNumber
      pickupAt DateTime
    outcome NoCapacity
      description "No pickup slot on the requested day"
  ability Quote safe
    address Address
    returns
      price Decimal
```

- A `system` belongs to the application, also when its file is imported into a module or feature. It names the business system, not a provider interface; an extension or implementation owns how.
- Everything under `ability` is optional and in any order: `description`, typed inputs, `returns`, `outcome`, `answers later`, `implementation`. Ability names are unique within a system, ignoring case (`PLAY0983`).
- Declaring an ability selects ESM v50; models without one keep their bytes and version.

## Effect classes

Write the class after the ability name. It says what a retry may do when an attempt's outcome is unknown.

| Class | Meaning | Unknown outcome |
| --- | --- | --- |
| `safe` | No external effect, such as a lookup | Retry freely |
| `idempotent` | Same idempotency key has the effect once | Retry with the same key |
| `once` | A repeat may repeat the effect | Never retry; stop for reconciliation |

`once` is the default because it is never wrong. Do not mark an ability `idempotent` unless the system really deduplicates. Any other class is `PLAY0984`.

## Inputs, returns and outcomes

- Inputs use the operation-input grammar: concepts, enums, composite types, collections, `optional`. Not `identifier` or `generated`, no subject modifier. A keyword-named input is written with `@` (`@outcome String`).
- `returns` holds at least one typed field; an ability without it returns nothing. Every type must be declared (`PLAY0986`).
- An `outcome <Name>` is a business result other than success, the external counterpart of a command refusal. It has a name and description, no payload. It cannot be named `next`, `refused`, `failed`, `returned` or `expired` (`PLAY0985`); those are invocation outlets.

## Answers that arrive later

`answers later` marks an ability whose answer arrives after the invocation returns (a person approves in chat, a partner confirms by webhook). Workflows and `approval ... via` use these.

## Implementation

An ability owns at most one `file` or tagged fence, directly or in an `implementation` wrapper (`PLAY0494` when combined). Its result is exactly one of: values matching `returns`, a declared outcome, or a failure. A hints-only block is intent and leaves the ability unrealized.

## The one `invokes` verb

`invokes` is the single verb for asking a command to run. The old forms still compile with deprecation `PLAY0397` and a repair; never author them.

| Before | After |
| --- | --- |
| `entering send <Command>` | `entering invokes <Command>` |
| `entering start <Workflow>` | `entering starts <Workflow>` |
| `then sends <Command>` | `then invokes <Command>` |
| `action <Name> sends <Command>` | `action <Name> invokes <Command>` |

## Invoke an ability

A reaction, a workflow action and a state's `entering` line ask a system for an ability with `invokes <System>.<Ability>`; the dotted name is what tells an ability from a command. Map every declared input exactly once with a compatible value (`PLAY1003`, `PLAY1004`, `PLAY1005`); the system and ability must exist (`PLAY1001`). The answer is handled where the call is made:

```play
reaction PickupBooking
  runs as system
  when OrderPacked
    parcelId
    shippingAddress
    invokes Carrier.BookPickup
      parcelId = parcelId
      address = shippingAddress
      on returned
        produces PickupBooked
          trackingNumber = $result.trackingNumber
      on NoCapacity
        produces PickupUnavailable
      on failed
        produces PickupBookingFailed
          reason = $failure.reason
```

- `on returned` reads `$result.<field>` from the ability's `returns`; `on failed` reads `$failure.reason`; `on <Outcome>` handles a declared outcome (`PLAY1002` when undeclared). A branch is declared once (`PLAY1006`); reading a branch value outside its branch is `PLAY1008`. The result becomes a fact only through the `produces` the model writes.
- Workflow action: `action BookPickup invokes Carrier.BookPickup attempts 3`, inputs on the lines under it (a literal, a property of the workflow's subject or an earlier node's result as `Node.field`), then edges `next -> ...`, `failed -> ...` and one edge per declared outcome. An outlet that can never be taken, such as `expired` on an ability that does not answer later, is `PLAY1007`. A node may only read the result of a node that always ran before it (`PLAY1009`).
- State machine: `entering invokes <System>.<Ability>` with inputs mapped below. `entering` may run again on redelivery, so the ability should be `safe` or `idempotent`. A state machine holds nothing to wait with, so an `answers later` ability there is `PLAY1011`; a reaction cannot invoke one either (`PLAY1010`).
- A `once` ability is never retried; `attempts` above one on its call site is the warning `PLAY1015`.
- Specifications script the answer with `given <System>.<Ability> returns` (with the returned fields), `given <System>.<Ability> answers <Outcome>` or `given <System>.<Ability> fails "<reason>"`, and assert the call with `then invokes <System>.<Ability>`. Naming an ability, outcome or field the system does not declare is `PLAY1017`. See [specifications](../../cratis-screenplay-specifications/SKILL.md).
- Operations in an Arc command can invoke abilities too (`invokes <System>.<Ability>` in an operation phase; Arc's `ISystemAbilities`); see Arc's command-operations page on abilities and the Screenplay operations page.

Invoking an ability selects ESM v51. Models that invoke none keep their bytes and version. Take the call's name, inputs and branch names from the page, never from memory.

## Realize a system with an extension

A system is realized either by code attached to its abilities (see Implementation) or by an extension package. Name the package on the system and map its settings:

```play
secret MailerApiKey
  description "The API key of the transactional mail service"

system Mailer
  extension "ghcr.io/cratis/extension-mail" version 2
  setting apiKey from secret MailerApiKey
  setting senderName = "Invoicing"
  activation on demand idle after 10 minutes
```

- A system that names an extension declares no abilities of its own (`PLAY1106`); it receives the contract's abilities, and the contract's triggers become application triggers a reaction uses as `when Payments.PaymentSucceeded`.
- `extension`, `setting` and `activation` are realization metadata: they change no bytes of the executable model.
- The contract, `screenplay.lock` and restore are in [extension packages](../../cratis-screenplay-extension-packages/SKILL.md); settings, named secrets and the environment keys are in [environments and secrets](../../cratis-environments-and-secrets/SKILL.md).

## What is admitted and what is not

According to the Screenplay documentation, these parse, print and validate but are not admitted yet, and binding reports `PLAY0268` naming Screenplay#1252: ability invocations in operation phases, an approval answered through an ability (`approval <Name> by <Policy> via <System>.<Ability> expires after 2 days`), `compute <Name>` nodes with `returns` fields and a C# body, a `$trigger` value, a `decide` that reads an ability or compute result, and a bare `entering` followed by a C# fence or `file` body. Author them when the model needs them, but report them as authored, not executable, and recheck the pages for the installed Screenplay version before claiming otherwise. Stage lists what it renders and runs in its [ESM feature matrix](https://github.com/Cratis/Stage/blob/main/Documentation/reference/esm-feature-matrix.md).

Read Screenplay's [systems and abilities](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md) page and [decision 0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md) for the contract.
