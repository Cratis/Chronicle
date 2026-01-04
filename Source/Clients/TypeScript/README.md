# @cratis/chronicle.contracts

TypeScript gRPC contracts for Chronicle.

## Installation

```bash
npm install @cratis/chronicle.contracts
# or
yarn add @cratis/chronicle.contracts
```

## Usage

This package provides Chronicle gRPC service definitions that can be loaded at runtime using `@grpc/proto-loader`.

```typescript
import { loadChronicleProtos, grpc } from '@cratis/chronicle.contracts';

// Load the proto definitions from bundled files
const packageDefinition = loadChronicleProtos();

// Access the services
const EventStores = packageDefinition.Cratis.Chronicle.Contracts.EventStores;

// Create a client
const client = new EventStores('localhost:5000', grpc.credentials.createInsecure());

// Call service methods
client.GetEventStores({}, (error, response) => {
    if (error) {
        console.error('Error:', error);
        return;
    }
    console.log('Event stores:', response.items);
});
```

You can also load proto files from a custom path if needed:

```typescript
import { loadChronicleProtosFromPath, grpc } from '@cratis/chronicle.contracts';

const packageDefinition = loadChronicleProtosFromPath('/path/to/proto/files');
```

## Proto Files

The proto files are generated from the Chronicle C# gRPC service contracts and are bundled with this package in the `proto` directory.

## License

MIT
