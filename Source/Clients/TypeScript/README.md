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
import { ChronicleConnection, ChronicleConnectionString } from '@cratis/chronicle.contracts';

// Create a connection using a connection string
const connection = new ChronicleConnection({
    connectionString: 'chronicle://localhost:35000'
});

// Connect to Chronicle
await connection.connect();

// Use the services with full type safety and IDE completion
const eventStores = await connection.eventStores.GetEventStores({});
console.log('Event stores:', eventStores.items);

// Clean up
connection.dispose();
```

### Connection Strings

Chronicle supports connection strings similar to database connection strings, providing a consistent way to configure connections:

```typescript
// Basic connection
const connection = new ChronicleConnection({
    connectionString: 'chronicle://localhost:35000'
});

// With client credentials (username:password)
const connection = new ChronicleConnection({
    connectionString: 'chronicle://myuser:mypassword@localhost:35000'
});

// With API key authentication
const connection = new ChronicleConnection({
    connectionString: 'chronicle://localhost:35000?apiKey=your-api-key-here'
});

// With TLS disabled (for development)
const connection = new ChronicleConnection({
    connectionString: 'chronicle://localhost:35000?disableTls=true'
});
```

### Development Connection

For local development, use the built-in development connection with default credentials:

```typescript
import { ChronicleConnectionString } from '@cratis/chronicle.contracts';

const connection = new ChronicleConnection({
    connectionString: ChronicleConnectionString.Development
});
```

The development connection string uses:
- **Client ID**: `chronicle-dev-client`
- **Client Secret**: `chronicle-dev-secret`
- **Host**: `localhost:35000`

These are the default development credentials that Chronicle Kernel accepts when running in development mode.

### Working with Connection Strings

```typescript
import { ChronicleConnectionString } from '@cratis/chronicle.contracts';

// Parse a connection string
const connStr = new ChronicleConnectionString('chronicle://localhost:35000');

// Access connection details
console.log(connStr.serverAddress.host); // 'localhost'
console.log(connStr.serverAddress.port); // 35000

// Create new connection strings with modifications
const withCreds = connStr.withCredentials('myuser', 'mypassword');
const withApiKey = connStr.withApiKey('my-api-key');

// Convert to string
console.log(withCreds.toString()); // chronicle://myuser:mypassword@localhost:35000
```

### Authentication

Chronicle supports two authentication modes. When using Client Credentials, the TypeScript client automatically obtains a bearer token from the authentication authority using OAuth 2.0 client_credentials flow.

#### Client Credentials (OAuth2 client_credentials flow)

The client automatically obtains and refreshes bearer tokens from the Chronicle server (or a custom authority):

```typescript
const connection = new ChronicleConnection({
    connectionString: 'chronicle://client-id:client-secret@localhost:35000'
});

// With custom authority
const connection = new ChronicleConnection({
    connectionString: 'chronicle://client-id:client-secret@localhost:35000',
    authority: 'https://my-auth-server.com',
    managementPort: 8080 // Optional, defaults to 8080
});
```

The token is automatically included as a Bearer token in the authorization header for all gRPC calls.

#### API Key

```typescript
const connection = new ChronicleConnection({
    connectionString: 'chronicle://localhost:35000?apiKey=your-api-key'
});
```

### Using Individual Services

You can also import and use services directly:

```typescript
import { EventStoresClient } from '@cratis/chronicle.contracts';
import * as grpc from '@grpc/grpc-js';

const client = new EventStoresClient(
    'localhost:35000',
    grpc.credentials.createInsecure()
);

const response = await client.GetEventStores({});
console.log('Event stores:', response.items);
```

### Configuration Options

```typescript
const connection = new ChronicleConnection({
    connectionString: 'chronicle://localhost:35000',
    
    // Optional: Override credentials from connection string
    credentials: grpc.credentials.createSsl(),
    
    // Optional: connection timeout in ms
    connectTimeout: 10000,
    
    // Optional: message size limits
    maxReceiveMessageSize: 1024 * 1024 * 10, // 10MB
    maxSendMessageSize: 1024 * 1024 * 10, // 10MB
    
    // Optional: for request tracking
    correlationId: 'my-correlation-id',
    
    // Optional: Custom authentication authority URL
    // If not set, uses Chronicle server as the authority
    authority: 'https://my-auth-server.com',
    
    // Optional: Management port for authentication endpoint (defaults to 8080)
    managementPort: 8080
});
```

### Legacy Server Address

For backward compatibility, you can still use `serverAddress`:

```typescript
const connection = new ChronicleConnection({
    serverAddress: 'localhost:35000'
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
