<!-- cratis-ai-managed: skills/cratis-engineering-post-mortem/references/public-safe-wording.md -->
# Public-safe wording

A post-mortem and the issues filed from it may be public. Run this pass last,
over the review and over every issue body.

## Remove or generalize

- Secrets of any kind: tokens, keys, passwords, connection strings. Write
  `<redacted: kind>`; never the value, not even partly.
- Internal hostnames, addresses, cluster and bucket names, and local
  filesystem paths. Use a role such as `<database-cluster>` or a repository
  relative path.
- Personal names, emails and handles. Use roles: "the on-call engineer", "the
  release owner".
- Customer names, and private repository or ticket titles that reveal
  confidential work.
- Run, job and commit identifiers that only resolve in a private system. Keep a
  stable public link, or write `<run-link>`.
- Speculation about a third party's fault. State what was observed.

## Before and after

| Before | After |
|---|---|
| Alex approved it without review | The change was merged without review |
| Failed on db-07.corp.example | Failed on `<database-cluster>` |
| Read from a local home directory path | Read from `<local-config>` |
| Connection string pasted from the log | `<redacted: connection string>` |
| The vendor's bug broke our upload | Uploads failed after the tool upgrade; the cause is Unverified |

## Scan checklist

Search the review and each issue body for these before publishing. A match is
either removed or justified as a public link.

- Local path prefixes for user home directories.
- `@` followed by a name, and email addresses.
- The words `token`, `key`, `password`, `secret`, `bearer`.
- IPv4 addresses and internal hostname suffixes.
- Long hexadecimal strings, which are often commit or key material.
- Private tracker identifiers that do not resolve publicly.

Finish with a **Redaction note** in the review naming the categories removed,
never the values.
