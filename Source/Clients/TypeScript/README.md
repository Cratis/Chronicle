# @cratis/chronicle.contracts

TypeScript gRPC contracts for Chronicle with full type safety and IDE support.

## Installation

```bash
npm install @cratis/chronicle.contracts
# or
yarn add @cratis/chronicle.contracts
```

## Usage

This package provides strongly-typed Chronicle gRPC service clients generated from proto definitions using ts-proto.

### Quick Start

```typescript
import { ChronicleConnection } from '@cratis/chronicle.contracts';

// Create a connection
const connection = new ChronicleConnection({
    serverAddress: 'localhost:5000'
});

// Connect to Chronicle
await connection.connect();

// Use the services with full type safety and IDE completion
const eventStores = await connection.eventStores.GetEventStores({});
console.log('Event stores:', eventStores.items);

// Clean up
connection.dispose();
```

### Using Individual Services

You can also import and use services directly:

```typescript
import { EventStoresClient } from '@cratis/chronicle.contracts';
import * as grpc from '@grpc/grpc-js';

const client = new EventStoresClient(
    'localhost:5000',
    grpc.credentials.createInsecure()
);

const response = await client.GetEventStores({});
console.log('Event stores:', response.items);
```

### Configuration Options

```typescript
const connection = new ChronicleConnection({
    serverAddress: 'localhost:5000',
    credentials: grpc.credentials.createSsl(), // Optional: use SSL
    connectTimeout: 10000, // Optional: connection timeout in ms
    maxReceiveMessageSize: 1024 * 1024 * 10, // Optional: 10MB
    maxSendMessageSize: 1024 * 1024 * 10, // Optional: 10MB
    correlationId: 'my-correlation-id' // Optional: for request tracking
});
```

### Available Services

The `ChronicleConnection` provides access to all Chronicle services:

- `eventStores` - Event store management
- `namespaces` - Namespace management
- `recommendations` - Recommendations
- `identities` - Identity management
- `eventSequences` - Event sequence operations
- `eventTypes` - Event type management
- `constraints` - Event constraints
- `observers` - Observer management
- `failedPartitions` - Failed partition handling
- `reactors` - Reactor management
- `reducers` - Reducer management
- `projections` - Projection management
- `readModels` - Read model operations
- `jobs` - Job management
- `eventSeeding` - Event seeding
- `server` - Server information

### Type Safety

All services are fully typed with TypeScript interfaces generated from proto definitions, providing:

- **IntelliSense** in your IDE
- **Compile-time type checking**
- **Auto-completion** for all methods and parameters
- **Type inference** for request and response objects

## License

MIT
