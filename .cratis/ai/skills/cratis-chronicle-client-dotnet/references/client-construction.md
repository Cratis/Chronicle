<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/client-construction.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Construct a standalone client

Source: Cratis/Chronicle:
`Source/Clients/DotNET/ChronicleClient.cs` and `IChronicleClient.cs`.

The options constructor takes these optional collaborators:
`IClientArtifactsProvider artifactsProvider`, `IServiceProvider serviceProvider`,
`IIdentityProvider identityProvider`, `ICorrelationIdAccessor correlationIdAccessor`,
`IEventStoreNamespaceResolver namespaceResolver`, `ILoggerFactory loggerFactory`,
and `INamingPolicy namingPolicy`. All default to null at the call boundary and
are replaced with the standalone defaults described in the skill. A reactor
with constructor dependencies needs a real container; pass a logger factory to
see registration failures rather than a silent client.

The following is a connection excerpt assuming a configured `loggerFactory`;
use a development kernel and keep the process alive for observation:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Registrations;

var options = ChronicleOptions.FromConnectionString("chronicle://localhost:35000");
using var client = new ChronicleClient(options, loggerFactory: loggerFactory);
var store = await client.GetEventStore("TestStoreCS");
var outcome = await store.WaitForRegistration();
if (!outcome.IsSuccess)
{
    throw outcome.Failure ?? new InvalidOperationException(string.Join(", ", outcome.Failures));
}
```

`WaitForRegistration` returns after either success or failure; it is not a reactor
subscription wait. `IChronicleClient` exposes `Options`, `CausationManager`,
`GetEventStore(name, namespace)`, `GetEventStores(cancellationToken)`, and
`EvictEventStores()`. It has no `Connect`: getting a store connects the client;
`eventStore.Connection.Connect()` is the explicit connection form.
