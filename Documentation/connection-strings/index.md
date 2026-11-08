---
title: Cratis Chronicle connection string setup
description: Cratis Chronicle connection strings specify server addresses, authentication, and TLS options. Choose a setup guide or look up the URI format.
---

<a id="connection-strings"></a>

A Cratis Chronicle connection string specifies the server addresses, authentication, and TLS options a client uses to connect. Keep it in configuration when the endpoint changes between environments, or pass it directly when constructing a client yourself.

For a .NET application using `AddCratisChronicle`, start with [connection string configuration](configuration). That guide shows how to bind settings from files or environment variables and explains which value wins when you also supply a callback or a named connection string.

If you construct `ChronicleClient` yourself, follow [.NET client usage](dotnet-client) for direct constructors, the fluent builder, and safe logging. Use [server support](server.md) to look up the URI format, authentication parameters, and multiple-server options.

