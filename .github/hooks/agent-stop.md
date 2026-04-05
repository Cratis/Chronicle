---
on: agentStop
---

# Agent Stop — Release Build

When the agent finishes a session, always run a full repository clean build in **Release** configuration and only stop when the result is zero warnings and zero errors.

## Steps

1. **Clean from repository root**:
   ```
   dotnet clean
   ```

2. **Build from repository root in Release mode**:
   ```
   dotnet build -c Release
   ```

3. **If any build fails or any warnings are reported**:
   - Report the full compiler output.
   - Fix all errors and warnings before considering the session complete.
   - Re-run `dotnet clean` and `dotnet build -c Release` until both pass with zero warnings.

## Rules

- Always run `dotnet clean` followed by `dotnet build -c Release` from repository root.
- A session is not complete until `dotnet build -c Release` exits with code `0` and **zero** warnings.
- Treat Release-only warnings (nullable annotations, unused variables stripped by the analyzer, etc.) as errors — fix them.
- **Never** use `/clp:ErrorsOnly` or any flag that suppresses warning output. Warnings that are hidden are warnings that are never fixed. Always let the full diagnostic output through so that zero warnings can be confirmed.
