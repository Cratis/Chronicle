# Configuration

The CLI resolves its connection to a Chronicle server through a layered configuration system. You can also create named contexts to switch between multiple environments.

## Connection Resolution

The CLI configuration file is stored at `~/.cratis/config.json`.

The CLI determines which server to connect to using the following precedence (highest to lowest):

1. `--server` flag on the command
2. `CHRONICLE_CONNECTION_STRING` environment variable
3. Active context in the configuration file
4. Default: `chronicle://localhost:35000`

## Connection String Format

```text
chronicle://<client-id>:<client-secret>@<host>:<port>/?disableTls=true
```

See [Connection Strings](../connection-strings/index.md) for the full connection string reference.

For local development without TLS:

```shell
cratis config set server "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000/?disableTls=true"
```

## Configuration Commands

### Show current configuration

```shell
cratis config show
```

### Set a value

Valid keys: `server`, `event-store`, `namespace`, `client-id`, `client-secret`.

```shell
cratis config set server chronicle://myhost:35000
cratis config set event-store MyStore
cratis config set namespace Production
```

### Find the configuration file

```shell
cratis config path
```

## Named Contexts

Contexts let you store connection details for multiple environments and switch between them.

### Create a context

```shell
cratis context create dev --server "chronicle://localhost:35000/?disableTls=true" -e MyStore
cratis context create prod --server "chronicle://prod-host:35000" -e Production
```

The first context you create becomes the active context automatically.

### Switch contexts

```shell
cratis context set prod
```

### List contexts

```shell
cratis context list
```

### Show the active context

```shell
cratis context show
```

### Rename or delete a context

```shell
cratis context rename dev development
cratis context delete old-staging
```

You cannot delete the currently active context.

## Event Store and Namespace Defaults

Most commands require an event store and namespace. You can set defaults so you don't have to pass them every time:

```shell
cratis config set event-store MyStore
cratis config set namespace Default
```

You can always override them per command:

```shell
cratis observers list -e OtherStore -n Production
```

## Authentication

### Client credentials

Set client credentials in the connection string or via config:

```shell
cratis config set client-id my-app
cratis config set client-secret my-secret
```

### User login

For user-level operations, log in with the password grant flow:

```shell
cratis login admin
```

This prompts for a password interactively. For scripted or non-interactive use:

```shell
cratis login admin --password "${CHRONICLE_PASSWORD}"
```

Check your authentication status:

```shell
cratis auth status
```

Log out and clear the cached token:

```shell
cratis logout
```
