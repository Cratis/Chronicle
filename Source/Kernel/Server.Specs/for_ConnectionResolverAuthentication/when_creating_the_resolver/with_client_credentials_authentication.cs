// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Server.for_ConnectionResolverAuthentication.when_creating_the_resolver;

/// <summary>
/// OAuth client credentials configured under storage.connectionResolution reach the HTTP resolver.
/// </summary>
public class with_client_credentials_authentication : given.a_configured_storage_connection_resolution
{
    void Establish() => Configure(new Dictionary<string, string?>
    {
        [$"{Resolution}:Resolver"] = "Http",
        [$"{Resolution}:Http:Endpoint"] = Endpoint,
        [$"{Resolution}:Http:Authentication:Type"] = "ClientCredentials",
        [$"{Resolution}:Http:Authentication:TokenEndpoint"] = "https://login.example/oauth2/token",
        [$"{Resolution}:Http:Authentication:ClientId"] = "chronicle",
        [$"{Resolution}:Http:Authentication:ClientSecret"] = "from-your-secret-store",
        [$"{Resolution}:Http:Authentication:Scope"] = "connections"
    });

    void Because() => CreateResolver();

    [Fact] void should_create_the_resolver() => _error.ShouldBeNull();
    [Fact] void should_hand_the_authentication_type_to_the_resolver() => _authenticationReachingTheResolver!.Type.ShouldEqual(HttpConnectionResolverAuthenticationType.ClientCredentials);
    [Fact] void should_hand_the_token_endpoint_to_the_resolver() => _authenticationReachingTheResolver!.TokenEndpoint.ShouldEqual(new Uri("https://login.example/oauth2/token"));
    [Fact] void should_hand_the_client_id_to_the_resolver() => _authenticationReachingTheResolver!.ClientId.ShouldEqual("chronicle");
    [Fact] void should_hand_the_client_secret_to_the_resolver() => _authenticationReachingTheResolver!.ClientSecret.ShouldEqual("from-your-secret-store");
    [Fact] void should_hand_the_scope_to_the_resolver() => _authenticationReachingTheResolver!.Scope.ShouldEqual("connections");
}
