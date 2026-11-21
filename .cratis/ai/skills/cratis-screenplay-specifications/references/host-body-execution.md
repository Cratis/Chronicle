<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/host-body-execution.md -->
# Host execution of validation and reducer bodies

Rule, `validate` and reducer bodies are opaque code. The standalone `screenplay` tool supplies no
provider, so a specification that reaches one is Unsupported, never passing. A host program can opt
in to running them. Reaction bodies are covered in
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
- **Reducers.** `ReduceAsync` receives the prior state, the event and `Occurred` from the fact (the
  clock is never read) and returns `Next(composite)` or `Delete()`. History folds in order, per
  read model and event-source identity; any Unsupported reducer returns the original world.
  Requires Screenplay 4.255.0 or later (`screenplay --version`).
- **Without a provider** nothing changes: opaque bodies are Unsupported and a specification that
  needs one never passes.
- **Limits.** Screenplay ships no C# executor; the host enforces API admission, isolation and
  resource limits, and capability `pure` is not a sandbox. Do not claim a target such as Stage
  executes these bodies: what Stage renders is stated in `cratis-stage-rendering-and-sandbox`.

Source: released `commands.md` (named rule bodies) and `readmodels.md` (reducer transitions).
