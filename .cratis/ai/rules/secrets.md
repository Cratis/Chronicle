---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/secrets.md -->

# Secrets never enter agent transcripts

A transcript is saved, summarized and shared, so a credential that is printed once is leaked.
Every line is tagged **[contract]** (binding) per the Three Levels of Authority in
[`general.md`](./general.md). The `cratis-guard-secrets` hook blocks the commands below
(see [`.cratis/ai/hooks/README.md`](../hooks/README.md)); the rule holds where no hook runs.

## Commands

- **[contract] Never run a command whose output contains credentials:** `kubectl get … -o json|yaml`
  or `kubectl describe` on pods, deployments, statefulsets, daemonsets, jobs, configmaps or
  secrets; `kubectl get secret`; `kubectl config view --raw`; `env`, `printenv`, `export -p`
  (including `env | …`); `docker inspect`; `gh secret`, `gh api …/secrets`, `gh auth token`;
  `vault read`, `vault kv get`; cloud secret-manager reads; `pulumi … --show-secrets`;
  reading kubeconfigs, `.env*` files, private keys or token files.
- **[contract] Project the field you need and say which one:** `-o jsonpath='{.spec.containers[*].image}'`,
  `-o custom-columns=…`, `kubectl get … -o name`, status fields, `kubectl logs`, `docker inspect --format`.
  Read a `.env.example`, never the `.env`.
- **[contract] The hook reads command text only.** It cannot see a command piped into a shell
  (`… | sh`, `bash -c "$(…)"`), a recursive search with no named file (`grep -r PASSWORD .`), or a
  value reached through a variable, alias or script. These stay your rule to keep; output redaction
  is the only backstop.

## Values

- **[contract] Never paste, echo or log a credential**, and never put one in a brief, handoff,
  issue, PR, message or commit. Reference a secret by name only (secret/key, environment variable).
- **[contract] When a value appears in output, stop.** Do not copy it onward. Report
  **SECURITY INCIDENT** with the file or tool-call location and the credential kind, and ask for
  rotation. Do not retry the command in another form.
- **[contract] Redact before archiving.** Scan and redact a work record before it is archived or
  shared (see [`local-work-artifacts.md`](./local-work-artifacts.md)).
- **[contract] The escape hatch is a human's.** `CRATIS_HOOKS_ALLOW_SECRET_READS=1` is set by the
  person who authorized the read, in the harness environment, never by an agent.
