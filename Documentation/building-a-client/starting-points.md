# Two ways to start

Before writing a line of idiomatic code, a new client needs typed bindings for Chronicle's gRPC
services. There are two ways to get them, and they're not equally good uses of your time.

## Option A: generate from the `.proto` files yourself

Chronicle's gRPC service and message definitions are code-first: they're written in C# using
[protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) attributes
(`[Service]`, `[Operation]`), and standard `.proto` files are extracted from the compiled
contracts assembly by a tool called `ProtoGenerator`. Those extracted files live in
`Source/Kernel/Protobuf/*.proto` in the Chronicle repository and are regenerated on every kernel
build — see [Protobuf Extraction](../contributing/clients/protobuf-extraction.md) for exactly how
that works.

Because they're standard proto3, any language with a gRPC/protobuf toolchain can generate client
bindings from them directly:

```bash
protoc \
  -I Source/Kernel/Protobuf \
  --go_out=. \
  --go-grpc_out=. \
  Source/Kernel/Protobuf/*.proto
```

This is the fully self-service path. Nobody has to grant you anything — the files are in the
repository. It's also the slower path: you own getting the `protoc` invocation right for your
language's plugin ecosystem, you own re-running it every time the kernel's contract changes, and
you own publishing and versioning the result yourself.

## Option B: ask the Cratis team for a generated package (recommended)

The alternative is to ask the Cratis team to turn on the same pipeline that already produces
TypeScript, Elixir, and Kotlin's contracts packages, for your language. Concretely, that pipeline:

1. Extracts `.proto` files from the kernel's contracts assembly (the step Option A does by hand).
2. Runs your language's code generator against them.
3. Publishes the result to the registry your ecosystem actually uses — npm, Hex, Maven Central,
   NuGet, wherever idiomatic packages for that language live.
4. Version-locks the published package to the kernel release it was generated from, and gates the
   whole release on a wire-compatibility check, so a contracts package can never ship out of step
   with the server it talks to.

This is precisely how it happened for every contracts package that exists today:

| Language | Contracts package | Registry |
|---|---|---|
| TypeScript | `@cratis/chronicle.contracts` | npm |
| Kotlin/Java | `io.cratis:chronicle-contracts` | Maven Central |
| Elixir | `cratis_chronicle_contracts` | Hex |
| Python | `cratis-chronicle-contracts` | PyPI |

Python is the newest addition, and it's worth noticing what stage it's at: the contracts package
exists and publishes on its own, but there's no idiomatic `Chronicle.Python` repo built on top of
it yet. That's expected — [layering an idiomatic client](./layering-an-idiomatic-client.md) on top
of the contracts package is a separate, later step, not something that has to land in the same
change as the contracts package itself.

None of these client repos generate or own their own contracts — the packages are built and
published from parallel jobs in Chronicle's own `publish.yml`, all consuming the same `.proto`
artifact, all gated behind the same `wire-compatibility` check. It happens fast, too: Elixir's
idiomatic client already depended on a published, versioned contracts package
(`{:cratis_chronicle_contracts, ">= 0.1.0"}`) in its very first commit.

### Why this is worth asking for

- **It's fast.** The generator, the packaging conventions, and the publish pipeline already exist
  and already work for three ecosystems. Adding a fourth is mostly configuration, not invention.
- **It stays correct without your attention.** The package versions in lockstep with the kernel
  and is protected by the wire-compatibility gate — you're not responsible for noticing that the
  kernel added a field or renamed a service.
- **It's published where your ecosystem expects it.** A hand-generated set of files sitting in a
  `vendor/` folder is not something other people in your language's community can `npm install` or
  `mix deps.get`. A registry package is.
- **It's the internal team's day-to-day workflow too.** Asking isn't a special-case request that
  routes around some other, more official process — it *is* the process the Cratis team already
  uses for every language it supports.

Reach out to the Cratis team to start that conversation. Once the contracts package exists and is
publishing on its own release cadence, move on to
[layering an idiomatic client on top of it](./layering-an-idiomatic-client.md).
