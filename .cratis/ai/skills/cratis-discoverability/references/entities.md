<!-- cratis-ai-managed: skills/cratis-discoverability/references/entities.md -->
# Entity names and definitions

## Contents

- Naming rules
- Shared identifiers
- Definition sentences
- Disambiguation

Only facts the Cratis repositories state are listed. If a product, URL or identifier is not here,
find it in the product's own README or docs before writing it; do not guess.

## Naming rules

- First mention on a page: "Cratis Chronicle". Later mentions may be "Chronicle". In package
  descriptions, README H1s and GitHub About text, always the prefixed form.
- The product is Cratis Direct. "Direct" and "Stagehand" are internal or older names.
- Cratis Build, Cratis Studio and Cratis Assurance are the three offers on cratis.no. In technical
  copy and metadata, name the product (Chronicle, Arc), not the offer.
- The product is not "Cratis.io". `cratis.io` is the docs host, canonical `https://www.cratis.io`.
- "Cratis" alone is both the company and the platform. Say "the Cratis platform" where the company
  could be meant.
- Cratis Studio maturity: follow `BRAND.md` (Preview) until the owner decides otherwise.

## Shared identifiers

| Item | Value |
| --- | --- |
| Organization `@id` | `https://cratis.no/#organization` |
| Company site | `https://cratis.no` |
| Docs (canonical host) | `https://www.cratis.io` |
| Blog | `https://blog.cratis.io` |
| GitHub organization | `https://github.com/Cratis` |
| NuGet profile | `https://www.nuget.org/profiles/Cratis` |
| npm organization | `https://www.npmjs.com/org/cratis` |
| YouTube | `https://www.youtube.com/@CratisStack` |

Product docs follow `https://www.cratis.io/<product>/`.

## Definition sentences

Use these as the opening sentence of a definitional page, README or package description. Adjust only
to fit the page; keep the facts.

| Name | Sentence | Docs page (under `https://www.cratis.io`) |
| --- | --- | --- |
| Cratis (platform) | Cratis is an open-source (MIT) event sourcing and CQRS platform for .NET, built around the Chronicle event-sourcing database. | `https://www.cratis.io/` |
| Cratis Chronicle | Cratis Chronicle is an open-source (MIT) event-sourcing database and runtime with a .NET kernel and clients for several languages. | `/chronicle/` |
| Cratis Arc | Cratis Arc is an open-source (MIT) CQRS application framework for ASP.NET Core with generated TypeScript proxies. | `/arc/` |
| Cratis Fundamentals | Cratis Fundamentals is the set of shared building blocks beneath the Cratis stack for .NET and TypeScript. | `/fundamentals/` |
| Cratis Studio | Cratis Studio is a collaborative environment for designing, visualizing and editing Screenplay event models. | `/studio/` |
| Cratis Direct | Cratis Direct is the managed control plane for the Cratis software factory: it turns GitHub signals into prioritized, governed AI work and reviewed pull requests. | none confirmed |
| Cratis Cast | Cratis Cast is a profile-site engine and directory for the people who build with Cratis, with verified, event-sourced profile pages. | `https://cast.cratis.io` |
| Cratis Screenplay | Cratis Screenplay is a business-oriented declarative language for specifying the desired functionality of an information system. | `/screenplay/` |
| Cratis Stage | Cratis Stage compiles Screenplay source, verifies modeled specifications, and renders or partially performs Cratis applications. | none confirmed |
| Cratis Prologue | Cratis Prologue captures selected signals from an existing system and interprets them into a provisional event model for review. | `/prologue/` |
| Cratis CLI | The Cratis CLI is a terminal tool for inspecting and diagnosing Cratis Chronicle. | `/cli/` |
| Chronicle Workbench | Chronicle Workbench is the web dashboard bundled with Cratis Chronicle for browsing events and operating a deployment. | `/chronicle/workbench/` |
| Chronicle MCP | The Chronicle MCP server connects an AI agent to a running Cratis Chronicle database over the Model Context Protocol. | `/chronicle-mcp/` |

Chronicle storage is pluggable (MongoDB by default, PostgreSQL, SQL Server, SQLite, in-memory).
Arc works without event sourcing; the Chronicle integration is optional.

## Disambiguation

- `@cratis/arc` is the browser client of Arc .NET; `@cratis/arc.core` is the Node server package.
- "Cast" alone is not the streaming protocol: write "Cratis Cast".
- A docs slug not in the table is unconfirmed. Check the docs site before linking it.
