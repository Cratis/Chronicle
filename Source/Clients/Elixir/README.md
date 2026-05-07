# cratis_chronicle_contracts

Generated Chronicle gRPC contracts for Elixir together with a small resilient
connection layer.

This package is intentionally non-idiomatic. Its job is to expose generated
protobuf and gRPC client modules that an idiomatic Elixir client can build on
top of later.

## Installation

```elixir
def deps do
  [
    {:cratis_chronicle_contracts, "~> 0.1.0"}
  ]
end
```

## What Is In The Package

- `Cratis.Chronicle.Contracts.ChronicleConnectionString` parses Chronicle
  connection strings.
- `Cratis.Chronicle.Contracts.ChronicleConnection` owns the gRPC channel,
  retries initial connection attempts, and reconnects when the adapter reports
  that the transport dropped.
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

## Connecting To Chronicle

```elixir
{:ok, connection} =
  Cratis.Chronicle.Contracts.ChronicleConnection.start_link(
    connection_string: "chronicle://localhost:35000?disableTls=true"
  )

:ok = Cratis.Chronicle.Contracts.ChronicleConnection.connect(connection)
{:ok, channel} = Cratis.Chronicle.Contracts.ChronicleConnection.channel(connection)
```

By default the connection layer uses the Mint gRPC adapter with retry support
enabled, and it schedules reconnect attempts when the adapter reports that the
connection process went down.

## Using Generated Stubs

After generating the Elixir sources, use the generated `*.Stub` modules under
`Cratis.Chronicle.Contracts.*` with the channel returned by
`ChronicleConnection.channel/1`.

For example, after generation you can call the generated services like this:

```elixir
{:ok, response} =
  Cratis.Chronicle.Contracts.EventStores.Stub.get_event_stores(
    channel,
    Google.Protobuf.Empty.new()
  )
```

## Publishing

The repository contains a dedicated GitHub Actions workflow for publishing this
package to Hex. The publish flow regenerates the Elixir sources from the current
proto files before running tests and `mix hex.publish --yes`.
