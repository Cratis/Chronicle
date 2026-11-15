---
applyTo: "**/*.sh,**/*.py,**/*.mjs,**/*.cjs,**/*.ps1,**/scripts/**,**/tools/**,**/.github/workflows/**,**/.cratis/ai/hooks/**"
paths:
  - "**/*.sh"
  - "**/*.py"
  - "**/*.mjs"
  - "**/*.cjs"
  - "**/*.ps1"
  - "**/scripts/**"
  - "**/tools/**"
  - "**/.github/workflows/**"
  - "**/.cratis/ai/hooks/**"
---
<!-- cratis-ai-managed: rules/scripting.md -->

# Scripts and tooling

- Write repository scripts, CI helpers and tooling in Node (`.mjs`, or `.ts` limited to erasable syntax: no enums, no parameter properties), using only `node:` built-ins (`fs.glob`, `util.parseArgs`, `fetch`, `node:test`). Node and the .NET SDK are the only runtimes every Cratis build machine and runner is guaranteed to have.
- Use a C# file-based app (`dotnet run --file tool.cs`, `#:package` pinned) when the script needs .NET or Cratis libraries or shares types with the product. Do not use one in hooks or on per-call hot paths.
- Hooks under `.cratis/ai/hooks` are POSIX sh compatible with bash 3.2, or Node. Read stdin with `IFS= read -r -d '' x || true`, never `$(</dev/stdin)`.
- Do not add Python scripts or `python3` workflow steps to Cratis repositories unless the project itself is Python (for example the Chronicle Python client). Existing Python scripts are ported to Node opportunistically: when you substantially change one, port it in the same PR. There is no bulk port.
- Agents: edit files with the edit/write tools, query JSON with `gh --jq`/`jq`, and use `node -e` or `node --input-type=module - <<'EOF'` for ad-hoc logic. Never `python3 -c`, and never rewrite files by script.
