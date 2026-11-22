<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/host-body-execution.md -->
# Host execution of verified bodies

Requires Screenplay 4.256.0 for policies and v1 handlers, 4.257.0 for the v2 provider seam and 5.4.0 for v2 source binding.

Rule, `validate`, policy, handler and reducer bodies are opaque code. The standalone `screenplay`
tool supplies no provider, so a specification that reaches one is Unsupported, never passing.
A host program can opt in to running resolved, content-verified bodies. Reaction bodies are covered in
[reaction bodies](../../cratis-screenplay-captures-and-reactions/references/reaction-forms.md#reaction-bodies-run-through-a-host-provider);
this page does not repeat them.

- **Seam.** The host supplies an `ISemanticBodyExecutionProviderFactory` and resolved bodies to
  `SemanticEvaluator` or `SemanticSpecificationRunner`, or runs the MCP server with
  `ScreenplayMcpServer.RunConfigured(new ScreenplayMcpRun(root, factory, cancellationToken), input, output)`.
  One isolated provider is created per execution or selected specification run and disposed before
  it returns.
- **Validation bodies.** `ValidateAsync` returns `Valid()`, `Invalid(params string[] messages)` or
  `Unsupported(diagnostic)`. Every rejection message is kept, in order, one error-severity failure
  per message. Command blocks and concept blocks both run, in the documented order, and every
  reached block runs once; the first Unsupported block stops execution. Execution the host cannot
  perform stays Unsupported, never a rejection.
- **Policies.** `AuthorizeAsync` returns Allow, Deny or Unsupported. Existing short-circuit
  gate order still applies; missing, unsupported or faulting execution fails closed, never as allow.
  A denial is an authorization outcome, not handler rejection or an execution fault.
- **Command handlers.** `HandleAsync` v1 returns ordered payloads matching the fixed,
  unconditional productions. The v2 contract (ESM v35) returns items
  matching `may produce`: validate declared event identities, payload shapes and destinations,
  including `for one of` collection membership. Validate the whole result before any append:
  an invalid later item makes the result Unsupported and commits nothing, even if earlier items
  were valid. Malformed output or admission refusal is Unsupported, not command rejection.
- **Handler faults and cancellation.** An admitted handler failure propagates
  `SemanticCommandBodyFaultException` with a typed fault kind (including host time/memory limits),
  not a domain denial/rejection. Unrequested cancellation is a typed Cancelled fault; cancellation
  requested through the supplied run token propagates the original `OperationCanceledException`.
  These fault semantics are specific to handlers, not a promise for other provider roles.
- **Reducers.** `ReduceAsync` receives the prior state, the event and `Occurred` from the fact (the
  clock is never read) and returns `Next(composite)` or `Delete()`. History folds in order, per
  read model and event-source identity; any Unsupported reducer returns the original world.
  Requires Screenplay 4.255.0 or later (`screenplay --version`).
- **Without a provider** nothing changes: opaque bodies are Unsupported and a specification that
  needs one never passes.
- **Limits.** Screenplay ships no C# executor; the host enforces API admission, isolation and
  resource limits, and capability `pure` is not a sandbox. Do not claim a target such as Stage
  executes these bodies: what Stage renders is stated in `cratis-stage-rendering-and-sandbox`.
  Bodies see only supplied command/context/caller values; do not invent missing timestamps,
  identity, reads or host services. Policy contexts omit audit causation; handler contexts do not
  provide view reads. Use typed-context discovery, not an invented wrapper API.

Source: Screenplay v6.32.0 `commands.md`, `readmodels.md`,
`ISemanticBodyExecutionProvider`, `SemanticCommandBodyExecution` and `SemanticHandlerResultValidation`.
