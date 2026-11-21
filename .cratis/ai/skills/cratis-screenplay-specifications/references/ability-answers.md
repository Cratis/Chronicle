<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/ability-answers.md -->
# Ability answers and `then invokes`

A reaction specification scripts what a system's ability answers while the reaction runs, then asserts the events and the call:

```play
given Carrier.BookPickup returns
  trackingNumber = "TRK-1"
given Carrier.BookPickup answers NoCapacity
given Carrier.BookPickup fails "carrier unavailable"
```

`returns` gives the returned fields, `answers` names a declared outcome, `fails` supplies the failure reason, and `then invokes <System>.<Ability>` asserts the call. Naming an ability, outcome or returned field the system does not declare is `PLAY1017`. A branch that reads a value the specification did not script is not silently passed. Read the Screenplay [specifications](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/specifications.md#ability-answers) page for the exact rules, and [systems and abilities](../../cratis-screenplay-automations-and-translations/references/systems-and-abilities.md) for the declaration.
