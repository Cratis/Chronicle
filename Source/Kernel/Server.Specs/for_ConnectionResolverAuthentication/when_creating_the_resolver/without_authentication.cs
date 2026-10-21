// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Server.for_ConnectionResolverAuthentication.when_creating_the_resolver;

/// <summary>
/// An HTTP resolver configured as before, without authentication, is unaffected by the resolver authentication the
/// kernel server registers: no token is ever asked for.
/// </summary>
public class without_authentication : given.a_configured_storage_connection_resolution
{
    void Establish() => Configure(new Dictionary<string, string?>
    {
        [$"{Resolution}:Resolver"] = "Http",
        [$"{Resolution}:Http:Endpoint"] = Endpoint,
        [$"{Resolution}:Http:Headers:X-Api-Key"] = "from-your-secret-store"
    });

    void Because() => CreateResolver();

    [Fact] void should_create_the_resolver() => _error.ShouldBeNull();
    [Fact] void should_bind_no_authentication() => _options.Storage.ConnectionResolution.Connection.Http.Authentication.Type.ShouldEqual(HttpConnectionResolverAuthenticationType.None);
    [Fact] void should_not_ask_any_provider_for_tokens() => _recordingProvider.DidNotReceiveWithAnyArgs().CanProvide(default!);
}
