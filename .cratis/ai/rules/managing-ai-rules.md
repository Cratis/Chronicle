---
applyTo: ".cratis/ai/**,.claude/**,.github/**,.agents/**,.pi/**,.cursor/**,.opencode/**,Source/**"
paths:
  - ".cratis/ai/**"
  - ".claude/**"
  - ".github/**"
  - ".agents/**"
  - ".pi/**"
  - ".cursor/**"
  - ".opencode/**"
  - "Source/**"
---
<!-- cratis-ai-managed: rules/managing-ai-rules.md -->

# Managing Cratis AI

`.cratis/ai` is the only canonical corpus. Edit rules, agents, prompts, skills,
hooks, and harness-specific source assets there.

Harness folders are adapters, not copies:

- Claude Code: `.claude`
- Codex: `.agents` and `AGENTS.md`
- GitHub Copilot: `.github`
- Cursor: `.cursor`
- OpenCode: `.opencode` and `AGENTS.md`
- Pi: `.pi` and `AGENTS.md`

Run `npm run setup --prefix Source/Harness.Setup` after adding or removing an
agent, prompt, or harness asset. Run the same command with `-- --check` to verify
that every adapter points to the canonical corpus.

The managed consumer path is `cratis ai install`. It resolves
`.cratis/ai.json`, installs selected content, records hashes in
`.cratis/ai.manifest.json`, and configures every selected harness. Native plugins
are independent single-harness integrations and do not provide that managed
lifecycle.

Do not add a second corpus, generated catalog tree, provenance ledger, or
repository inventory. Quality comes from focused verification in
`Source/Verification` and review of the source diff.
