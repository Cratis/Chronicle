---
title: Regulatory responsibilities
description: Map Chronicle's supported controls and your responsibilities under HIPAA, SEC/PCAOB, PCI DSS, and GDPR transfer rules without assuming certification.
---

You are deciding whether Chronicle can sit in a regulated system, and you need to know who does what. This page separates what Chronicle provides today from what you configure, what stays yours, and what is only planned.

:::caution[Not a certification]
Chronicle is a component. It is not certified for, and does not make your system compliant with, any regulation named here. Compliance belongs to the whole system: its deployment, its contracts, its people and its operations. Nothing on this page is legal advice.
:::

Sources reviewed on 2026-10-31. See [Keeping this page current](#keeping-this-page-current).

## How to read the tables

Every row carries one of four status labels.

| Label | Meaning |
|---|---|
| **Built in** | Chronicle does this without configuration. |
| **Optional configuration** | Chronicle can do this once you configure it. |
| **Planned** | Not available. A linked issue tracks it. |
| **Your responsibility** | Chronicle does not do this and cannot. Your application or deployment must. |

## Before you map anything: classify the record

Different records fall under different rules, and no single retention period applies to all Chronicle events.

- **Domain events** record what happened in your business.
- **Security audit logs** record who did what to which system.
- **Security Rule documentation** is the policies, procedures and assessments a regulation requires you to keep.
- **Audit workpapers** are the records an auditor keeps about an audit.

A domain event can serve as source evidence for the others. It does not become one of them by being stored in an event log.

## HIPAA

The HIPAA Security Rule states these requirements.

- **Audit controls.** You implement mechanisms that record and examine activity in information systems that contain or use electronic protected health information (45 CFR 164.312(b)).
- **Information system activity review.** You implement procedures to regularly review records of information system activity, such as audit logs, access reports and security incident tracking reports (45 CFR 164.308(a)(1)(ii)(D)).
- **Documentation retention.** You keep the documentation the Security Rule requires for 6 years from its creation or the date it last was in effect, whichever is later (45 CFR 164.316(b)(2)(i)). This is a rule about that documentation. It is not a general six-year rule for logs or events.

| Duty | Status | What this means with Chronicle |
|---|---|---|
| Record activity in the systems that hold ePHI | **Your responsibility** | Chronicle stores events. It does not produce an audit record of who read what. See [Read and access auditing](#read-and-access-auditing). |
| Review activity regularly | **Your responsibility** | Review happens in your IAM, SIEM or application audit. |
| Keep Security Rule documentation for 6 years | **Your responsibility** | Keep that documentation where you keep policies. Do not rely on the event log for it. |
| Compliance-grade retention and legal hold for events | **Planned** | See [#3798](https://github.com/Cratis/Chronicle/issues/3798) and [#925](https://github.com/Cratis/Chronicle/issues/925). |

## SEC and PCAOB (SOX-related)

Regulation S-X Rule 2-06 (17 CFR 210.2-06) requires an accountant to retain records relevant to an audit or review of an issuer's financial statements, including workpapers, for 7 years after the audit or review concludes.

This applies to the auditor's specified records. It does not apply to every domain event. Your domain events may be the source evidence that those records refer to. Chronicle has no compliance-grade retention or legal hold today, which is **Planned** in [#3798](https://github.com/Cratis/Chronicle/issues/3798) and [#925](https://github.com/Cratis/Chronicle/issues/925). Until then, retention and holds are **Your responsibility**.

## PCI DSS

This page does not summarize individual PCI DSS requirements. The standard's text was not checked against its primary source on the review date, so it is left out rather than paraphrased. Read Requirement 10 (logging and monitoring) in the current version of the standard published by the PCI Security Standards Council.

Two points follow from how Chronicle works and do not depend on that text.

- Chronicle does not produce a read or access audit trail. Logging, protection and review of logs are **Your responsibility**. See [Read and access auditing](#read-and-access-auditing).
- An event log is immutable. Do not put full card numbers or sensitive authentication data in events, event examples or test data. Chronicle's [PII encryption and erasure](./index.md) are not a substitute for keeping them out.

## GDPR: international transfers and residency

Chapter V of Regulation (EU) 2016/679 (Articles 44 to 49) sets the conditions for transferring personal data to a third country or an international organisation.

- **General principle.** A transfer takes place only if the conditions of Chapter V are met (Art. 44).
- **Adequacy.** A transfer may take place where the Commission has decided that the country, territory, sector or organisation ensures an adequate level of protection (Art. 45).
- **Appropriate safeguards.** Without an adequacy decision, a transfer needs appropriate safeguards and enforceable data subject rights and effective legal remedies. The safeguards Art. 46 lists include binding corporate rules, standard data protection clauses and approved codes of conduct (Art. 46, with Art. 47 for binding corporate rules).
- **Derogations.** Without either, a transfer may rely on one of the specific conditions in Art. 49, such as explicit consent after being informed of the risks.

Chapter V is a transfer regime. It is not a blanket rule that data must stay in the EU. Articles 45, 46 and 49 do not name encryption as a basis for a transfer, so encrypting or crypto-shredding data does not make a transfer lawful and is not evidence of where data is stored.

| Duty | Status | What this means with Chronicle |
|---|---|---|
| Decide where data may be stored and processed | **Your responsibility** | An event store and a namespace are logical boundaries. They are not a jurisdiction. |
| Choose and document the transfer basis | **Your responsibility** | Chronicle cannot know who receives your data. |
| Erase a subject's personal data on request | **Built in** (for protected values) | See [Erasing a subject](./erasing-a-subject.mdx) and [The encryption key lifecycle](./key-lifecycle.mdx#erasure-reaches-every-event-store-in-the-namespace). |

## Where your data goes: the boundary inventory

Chronicle is configured with primary storage once per kernel deployment. The storage type and connection details come from [configuration](/chronicle/hosting/configuration/storage/). Chronicle cannot infer or attest a physical jurisdiction from a connection string. Region, replicas, failover, backup location, administrators and subprocessors are **Your responsibility**.

| Boundary | What is stored or sent | Who decides location | Who decides retention | Who controls access | What evidence exists |
|---|---|---|---|---|---|
| Primary storage | Events and read models held by the kernel | You | You (no compliance-grade retention yet) | You, through the storage provider and the kernel's authentication | Your storage provider's records |
| Compliance key store | Encryption keys for protected values, optionally in a separate store | You | Key erasure, see [the key lifecycle](./key-lifecycle.mdx#what-chronicle-records-and-what-is-still-yours) | You | See [the key lifecycle](./key-lifecycle.mdx#what-chronicle-records-and-what-is-still-yours) |
| Encryption certificate | A certificate file you supply | You | You | You | Your own records |
| Read model sinks | Projected data the sink receives | You | The sink | The sink | The sink's own audit |
| Webhooks, external observers and reactors | Events delivered to the recipient | The recipient | The recipient | The recipient | The recipient's own audit |
| OpenTelemetry export | Telemetry the kernel exports, as configured | You | Your telemetry backend | Your telemetry backend | Your telemetry backend |
| Backups and restores | Copies of the above | You | You | You | Your backup tooling |
| Application exports | Whatever your application writes out | You | You | You | Your application |

For configuration, see [Compliance storage](/chronicle/hosting/configuration/compliance-storage/), [Encryption certificate](/chronicle/hosting/encryption-certificate/) and [OpenTelemetry](/chronicle/hosting/configuration/open-telemetry/).

## What Chronicle provides for key storage

| Capability | Status | Notes |
|---|---|---|
| Keep encryption keys in a store separate from the primary storage | **Optional configuration** | Configure it with [compliance storage](/chronicle/hosting/configuration/compliance-storage/). Moving existing keys has a migration setting. Read the page before you turn on a dedicated store. |
| Vault and Azure Key Vault key stores | **Optional configuration** | Azure Key Vault purges the soft-deleted secret when a key is erased. If the vault refuses the purge, for example because of purge protection, the erasure fails loudly instead of appearing to succeed. |
| Replication, backups and audit on the key store's own side | **Your responsibility** | Provider-side copies are outside Chronicle. What erasure guarantees on each key store is tracked in [#4982](https://github.com/Cratis/Chronicle/issues/4982). |
| The encryption certificate | **Your responsibility** | You supply the file path. See [Encryption certificate](/chronicle/hosting/encryption-certificate/). |
| Revising an event | **Built in** | A revision applies compliance to the revised content. See [Migrations and revisions release strictly](./key-lifecycle.mdx#migrations-and-revisions-release-strictly). |

## Read and access auditing

When authentication is enabled, the kernel requires an authenticated caller. When it is disabled, every request is allowed. Chronicle has no authorization model that distinguishes roles, actions, event stores, namespaces, subjects or purposes, so authentication is not least privilege. Authorization on the kernel side is **Planned** in [#1965](https://github.com/Cratis/Chronicle/issues/1965).

The `CausedBy` identity on an append is supplied by the calling client. It records what the client asserts, which is often a service rather than the end user. Reads do not produce a durable record of actor, purpose, resource and outcome. That record is **Your responsibility**, through your IAM, SIEM or application audit. Actor-attributed administration of key erasure is **Planned** in [#3793](https://github.com/Cratis/Chronicle/issues/3793).

## Backups and restores

A restore taken before an erasure or redaction can bring the erased data back. Chronicle does not detect or enforce re-erasure after a restore, so this is **Your responsibility**. Related planned work is tracked in [#5221](https://github.com/Cratis/Chronicle/issues/5221).

This is guidance, not a Chronicle feature.

1. Keep a ledger of every erasure and redaction outside the backup set.
2. Block traffic to Chronicle before the restore.
3. Restore the certificate, the primary storage and the key stores together.
4. Replay every erasure and redaction made after the backup was taken.
5. Verify before reads and egress resume.

## Downstream egress

Observers, reactors, webhooks, sinks and application exports send data to recipients that control their own retention, location and deletion. That is **Your responsibility**. Opt-in encrypted or redacted delivery for webhooks is **Planned** in [#5132](https://github.com/Cratis/Chronicle/issues/5132).

For what erasure and redaction reach, see [Erasing a subject](./erasing-a-subject.mdx#what-you-still-have-to-do) and [Event redaction](../events/redaction.mdx).

## Known limits

These open issues describe gaps that affect regulated use.

- [#4978](https://github.com/Cratis/Chronicle/issues/4978): An erased encryption key can still decrypt personal data on cached and unvalidated release paths.
- [#4979](https://github.com/Cratis/Chronicle/issues/4979): Encryption key saves and erasure fence writes are not atomic or monotonic on any key store.
- [#4980](https://github.com/Cratis/Chronicle/issues/4980): Materialized read models are not rebuilt after redaction or revision when their key differs from the event's source.
- [#4599](https://github.com/Cratis/Chronicle/issues/4599): Deferred projection futures persist `[PII]` and `[Encrypted]` child values in plaintext.

For the threat model, see [The threat model](./key-lifecycle.mdx#the-threat-model).

## What not to say about Chronicle

Do not describe Chronicle as "HIPAA compliant", "SOX compliant", "PCI compliant" or "GDPR compliant". Do not describe an event store or a namespace as a residency boundary. Do not describe crypto-shredding as proof that data left a jurisdiction.

## Sources

- HHS Security Rule, 45 CFR Part 164 Subpart C: [164.308](https://www.ecfr.gov/current/title-45/section-164.308), [164.312](https://www.ecfr.gov/current/title-45/section-164.312) and [164.316](https://www.ecfr.gov/current/title-45/section-164.316).
- SEC Regulation S-X, [17 CFR 210.2-06](https://www.ecfr.gov/current/title-17/section-210.2-06).
- Regulation (EU) 2016/679 (GDPR), Chapter V, Articles 44 to 49: [EUR-Lex](https://eur-lex.europa.eu/eli/reg/2016/679/oj).
- PCI DSS: the current version from the [PCI Security Standards Council document library](https://www.pcisecuritystandards.org/document_library/).

## Keeping this page current

- **Reviewed on:** 2026-10-31.
- **Owner:** Chronicle maintainers.
- **Process:** a pull request that changes the compliance code under `Source/Kernel`, the Vault or Azure Key Vault key stores, the kernel authentication code or `Documentation/compliance/**` rechecks the tables on this page. The source links are reviewed at least once a year.
