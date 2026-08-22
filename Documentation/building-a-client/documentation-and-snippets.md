# Documentation and snippets

Chronicle's docs are published as one site, aggregated at build time from a `Documentation/`
folder in each product repo — including every client repo. A new client needs to shape its
`Documentation/` folder the same way from day one, because the shared Chronicle pages actively rely
on every participating client having one.

This page is an orientation, not the full mechanics — those are already written down in
[Contributing to Clients](../contributing/clients/#shared-documentation-snippets). Read
that page for the complete rules on snippet IDs, CI validation, and adding a client to the shared
tab system. What follows is the shape to start from.

## The standard shape

```text
Documentation/
├── toc.yml
├── index.md                    landing page for this client's docs
├── getting-started.md
├── client-snippets/**          source-only — never becomes a public page
└── <client-specific-pages>     install, connection setup, framework integration, troubleshooting
```

`Documentation/client-snippets/**` is the important one to get right early: it's not a docs page
at all, it's the source for the language-specific code shown in the *shared* Chronicle docs. A
shared page says:

```mdx
<ChronicleClientTabs snippet="events/appending/example" />
```

and the site looks for a matching snippet ID under every registered client's snippet root — your
client shows up in that tab group the moment
`Documentation/client-snippets/events/appending/example.md` exists, with a single fenced code block
in your language. No other wiring makes a client opt-in; the file's presence is the whole
mechanism.

## Why concepts live in the shared docs, not in your repo

Resist the pull to write your own "Events" or "Projections" page inside your client's docs. Those
concepts are the same across every client — what differs is only the code that expresses them. A
client-specific page is for things that are genuinely different per client: installation, how a
connection gets configured in your language's idioms, framework/hosting integration, decorators or
annotations, and troubleshooting. Everything else belongs in the shared Chronicle pages, with your
code shown through a snippet.

If your client doesn't support a workflow yet, that fact belongs *in the snippet*, not as prose on
the shared page — an explicit "this client doesn't support this yet" snippet keeps the claim next
to the code, where whoever adds the capability is already looking, instead of rotting silently in a
shared page nobody remembers to update.

## Keep a validator in CI

Every existing client repo runs a snippet validator (`Documentation/validate-client-snippets.py` in
today's clients) that compiles every snippet against the real client source, wired into a CI
workflow that triggers on changes to either `Documentation/client-snippets/**` or `Source/**`. This
is what stops a client API change from silently leaving the shared docs' code examples broken —
without it, a snippet is just a string nobody checks. Build the equivalent for your language before
your first snippet ships, not after the first drift is reported.

## Two parallel trees, if your artifact serves two languages

Chronicle.Kotlin serves both Kotlin and Java from a single published artifact, and its
`Documentation/` folder reflects that directly: `client-snippets/` for Kotlin and a fully parallel
`client-snippets-java/` for Java, with every subfolder mirrored between the two. If your client
does the same — one package, two idiomatic surfaces — plan for two snippet trees from the start
rather than retrofitting a second one later.

## Getting a new client wired into the published site

Once your `Documentation/` folder exists, getting it into the published site alongside the other
four clients is a short, concrete checklist — see
[Contributing to Clients → Add another Chronicle client](../contributing/clients/#add-another-chronicle-client)
for the exact steps (registering the repo, adding it to the client-docs config, wiring the sync
workflow). That's also the point to talk to the Cratis team again if you haven't already — getting
a new client's docs live on the site is a one-time setup they can do quickly.
