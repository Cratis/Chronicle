---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/secrets.md -->

# Secrets never enter agent transcripts

Printed credentials leak into saved transcripts. All lines are **[contract]** (binding;
see [`general.md`](./general.md)). The [hook](../hooks/README.md) enforces these rules;
they also hold without it.

## Commands

- **[contract] Never run a command whose output contains credentials:** `kubectl get … -o json|yaml`
  or `kubectl describe` on pods, deployments, statefulsets, daemonsets, jobs, configmaps or
  secrets; `kubectl get secret`; `kubectl config view --raw`; `env`, `printenv`, `export -p`
  (including `env | …`); `docker inspect`; `gh secret`, `gh api …/secrets`, `gh auth token`;
  `vault read`, `vault kv get`; cloud secret-manager reads; `pulumi … --show-secrets`;
  `cratis config get … --show-secret`; reading kubeconfigs, `.env*` files, private keys or token files.
- **[contract] Project the field you need and say which one:** `-o jsonpath='{.spec.containers[*].image}'`,
  `-o custom-columns=…`, `kubectl get … -o name`, status fields, `kubectl logs`, `docker inspect --format`.
  Read a `.env.example`, never the `.env`.
- **[contract] Command-text checks cannot see** shell-piped code (`… | sh`, `bash -c "$(…)"`),
  recursive searches without filenames (`grep -r PASSWORD .`), or paths hidden in variables,
  aliases or scripts. The rules still apply; output redaction is the backstop.
- **[contract] After a guard block, narrowing to literal non-credential files is supported remediation,
  not a workaround.** Follow `Use instead`; never disguise the same secret read.

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
