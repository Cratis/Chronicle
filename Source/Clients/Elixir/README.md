# cratis_chronicle_contracts

Generated Chronicle gRPC contracts for Elixir.

This package is intentionally non-idiomatic. Its job is to expose generated
protobuf and gRPC client modules that an idiomatic Elixir client can build on
top of later.

## Installation

Replace `MAJOR.MINOR.PATCH` with a published contracts release compatible with your Chronicle kernel:

```elixir
def deps do
  [
    {:cratis_chronicle_contracts, "~> MAJOR.MINOR.PATCH"}
  ]
end
```

Releases containing the bundled `VERSION` file report their release version to Mix, so Mix can enforce the requirement.

## Migrating to grpc 1.x

This contracts release requires Elixir 1.18 or later (its grpc 1.x dependencies need it), `grpc ~> 1.0` and `mint ~> 1.11`.
The generated Chronicle protobuf messages and stubs do not change, but your
application may need to update how it uses grpc:

- `GRPC.Stub.connect/2` still defaults to the Gun adapter. In grpc 1.x, `:gun`
  is optional: pass `adapter: GRPC.Client.Adapters.Mint` when connecting, or
  add `{:gun, "~> 2.4"}` to your application's dependencies if you use Gun.
- Remove `{GRPC.Client.Supervisor, []}` from your supervision tree. grpc 1.x
  starts its own client supervisor.
- If your application serves gRPC requests, move its server dependency to
  `grpc_server`; server modules are no longer included in `grpc`.

The `cratis_chronicle` Elixir client still requires `grpc ~> 0.11`, so Mix
resolves an older compatible contracts release for that client until the
client has its own grpc 1.x release. Do not force this contracts release into
an application using the older client.

See the [grpc 1.x changelog](https://github.com/elixir-grpc/grpc/blob/v1.0.5/grpc/CHANGELOG.md#v100-2026-06-15)
for the upstream migration details.

## What Is In The Package

- `lib/generated` contains the generated protobuf message modules and `*.Stub`
  gRPC client modules.

## Generating The Elixir Client

From the repository root:

```bash
cd Source/Clients/Elixir
bash ./generate-protos.sh
```

The script:

1. Copies `Source/Kernel/Protobuf` into `priv/protos`
2. Generates Elixir protobuf and gRPC modules into `lib/generated`

The generated files are not meant to be edited by hand.

## Using Generated Stubs

After generating the Elixir sources, use the generated `*.Stub` modules under
`Cratis.Chronicle.Contracts.*` with the channel returned by
your gRPC connection/channel implementation.

For example, after generation you can call the generated services like this:

```elixir
{:ok, response} =
  Cratis.Chronicle.Contracts.EventStores.Stub.all_event_stores(
    channel,
    Google.Protobuf.Empty.new()
  )
```

Function names follow the rpc names in the `.proto` files (`AllEventStores` becomes `all_event_stores`). Check the module name the generator produced for a service before copying a call.

## Publishing

The repository contains a dedicated GitHub Actions workflow for publishing this
package to Hex. The publish flow writes the release version to `VERSION`, regenerates
Elixir sources from the current proto files, and tests the package before publishing it.
