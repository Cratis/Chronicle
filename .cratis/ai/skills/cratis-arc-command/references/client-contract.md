<!-- cratis-ai-managed: skills/cratis-arc-command/references/client-contract.md -->
# The generated client contract

This reference holds the members of the generated `Command` proxy.

| Member | What it does |
| --- | --- |
| `route` | The generated route, including the configured API prefix |
| `roles` | The roles the command declares; empty when it declares none |
| `<property>` | Get or set one value; setting raises `propertyChanged` |
| `hasChanges` | True when any value differs from the initial values |
| `execute(allowedSeverity?, ignoreWarnings?)` | Sends the command; resolves to `CommandResult` |
| `validate()` | Runs authorization and validation on the server without the handler |
| `validateClientSide()` | Runs only the extracted rules locally; never touches the network |
| `setInitialValues(values)` | Sets the change-tracking baseline |
| `setInitialValuesFromCurrentValues()` | Rebaselines onto the current values |
| `revertChanges()` / `clear()` | Restore the baseline / reset everything |
