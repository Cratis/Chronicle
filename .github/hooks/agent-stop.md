---
on: agentStop
---

# Agent Stop — Release Build

When the agent finishes a session, automatically build every affected project in **Release** configuration to verify the delivered code compiles cleanly under production settings (stricter nullable enforcement, optimizer-driven warnings, etc.).

## Steps

1. **Identify affected projects** by running `git diff --name-only HEAD` (or `git status --short` for unstaged changes). Collect the set of unique project roots:
   - For `.cs` files: walk up the directory tree to find the nearest `.csproj` file.
   - For `.ts` / `.tsx` files: walk up the directory tree to find the nearest `package.json` that contains a `"build"` script.

2. **Build each affected .NET project** in Release mode:
   ```
   dotnet build <project-path> -c Release
   ```
   If no specific project can be identified, run `dotnet build -c Release` from the repository root.

3. **Build each affected TypeScript/React project**:
   ```
   yarn build
   ```
   Run from the package root that owns the changed files.

4. **If any build fails or any warnings are reported**:
   - Report the full compiler output.
   - Fix all errors and warnings before considering the session complete.
   - Re-run the Release build to confirm it is clean.

## Rules

- Always run the Release build — never skip it even if the Debug build already passed earlier in the session.
- A session is not complete until `dotnet build -c Release` (and `yarn build` if applicable) exits with code `0` and **zero** warnings.
- Treat Release-only warnings (nullable annotations, unused variables stripped by the analyzer, etc.) as errors — fix them.
- **Never** use `/clp:ErrorsOnly` or any flag that suppresses warning output. Warnings that are hidden are warnings that are never fixed. Always let the full diagnostic output through so that zero warnings can be confirmed.
