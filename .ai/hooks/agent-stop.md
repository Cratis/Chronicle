---
on: agentStop
---

# Agent Stop — Release Build and Specs

When the agent finishes a session, always run a full repository clean build in **Release** configuration and the relevant specs/tests for the affected projects, and only stop when the result is zero warnings, zero errors, and successful specs.

## Steps

1. **Clean from repository root**:
   ```
   dotnet clean
   ```

2. **Build from repository root in Release mode**:
   ```
   dotnet build -c Release
   ```

3. **Run specs/tests for every affected project**:
   - Use the project's existing test command.
   - If you cannot confidently isolate the affected scope, run the repository-level test command instead.

4. **If any build fails, any warnings are reported, or any specs/tests fail**:
   - Report the full compiler output.
   - Fix all errors, warnings, and failing specs before considering the session complete.
   - Re-run `dotnet clean`, `dotnet build -c Release`, and the affected specs/tests until all pass.

## Rules

- Always run `dotnet clean` followed by `dotnet build -c Release` from repository root.
- Always run the relevant specs/tests for every affected project before stopping.
- A session is not complete until `dotnet build -c Release` exits with code `0` and **zero** warnings.
- A session is not complete until the affected specs/tests exit with code `0`.
- Treat Release-only warnings (nullable annotations, unused variables stripped by the analyzer, etc.) as errors — fix them.
- **Never** use `/clp:ErrorsOnly` or any flag that suppresses warning output. Warnings that are hidden are warnings that are never fixed. Always let the full diagnostic output through so that zero warnings can be confirmed.
