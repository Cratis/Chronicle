# @cratis/chronicle.contracts

TypeScript gRPC contracts for Chronicle with full type safety and IDE support.

## Features

- **Promise-based API**: Modern async/await support for all unary RPC calls
- **AsyncIterable Streams**: Server streaming with `AsyncIterable` for easy iteration
- **Full Type Safety**: Strongly-typed clients and message types generated from proto definitions
- **64-bit Safety**: Proto `int64`/`uint64` values are represented as `bigint`
- **Zero Wrapper Code**: Direct client method calls without manual Promise wrapping

## Installation

```bash
npm install @cratis/chronicle.contracts
# or
yarn add @cratis/chronicle.contracts
```

## Usage

This package contains only the generated contracts: a `*Definition` for every Chronicle gRPC service and a TypeScript type for every message, produced by [ts-proto](https://github.com/stephenh/ts-proto) for [nice-grpc](https://github.com/deeplay-io/nice-grpc). It has no connection string parsing, authentication, or retry handling.

**Building an application?** Use the idiomatic TypeScript client, [`@cratis/chronicle`](https://www.npmjs.com/package/@cratis/chronicle), which is built on these contracts. Reach for this package only when you are building a client or tool of your own.

### Calling a service

The following excerpt shows the shape of a call. It is not a complete program: a real Chronicle server serves TLS on port `35000` — with a self-signed certificate in development — and expects a bearer token on every call, so you supply channel credentials and call metadata that fit your server.

```typescript
import { createChannel, createClient, ChannelCredentials } from 'nice-grpc';
import { EventStoresDefinition } from '@cratis/chronicle.contracts';

const channel = createChannel('localhost:35000', ChannelCredentials.createSsl());
const eventStores = createClient(EventStoresDefinition, channel);

const response = await eventStores.allEventStores({});
```

Each rpc in the `.proto` files becomes a camel-cased method on its client (`AllEventStores` becomes `allEventStores`). Unary calls return a `Promise`; server-streaming calls return an `AsyncIterable`.

### Type safety

Every service client and message is typed from the proto definitions, including `int64`/`uint64` fields represented as `bigint`.

## License

MIT
