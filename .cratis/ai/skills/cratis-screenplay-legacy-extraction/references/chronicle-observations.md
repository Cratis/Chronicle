<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/chronicle-observations.md -->
# Chronicle observations

For a source system that already runs on Chronicle. Complements static extraction; it is
read-only evidence, not authority to change anything.

Confirm permission, server context, event store, namespace and data sensitivity first. Use
the installed `cratis` CLI catalog for exact commands (`cratis-chronicle-cli-operations`, read-only; `cratis-chronicle-mcp-inspection` for MCP inspection) and scope every
read by identity, type, range or page. Check `cratis-screenplay-toolchain` `references/versions.md` for what the installed tools
support before relying on any inspection tool.

For the extraction question at hand, inspect only what is relevant: event schemas and
generations, bounded append evidence, projection definitions, observer subscriptions and
progress, failed partitions, read-model instances. Record target, observation time and
coverage with each evidence row. Use redacted locators, not copied personal payloads.

Read the evidence carefully:
- registered is not appended; declared is not deployed; stale is not absent;
- an empty view can mean lag or a failed observer;
- no matching events in one scanned range does not prove a behaviour never occurs;
- sequence numbers need their namespace and sequence identity;
- live observations do not prove unobserved rejection or race behaviour.

Never replay, retry, clear a quarantine, revise or redact merely to obtain evidence. Report
the cause and propose recovery separately for authorization. Reconcile runtime evidence with
source and expert testimony; keep contradictions and unknowns visible.
