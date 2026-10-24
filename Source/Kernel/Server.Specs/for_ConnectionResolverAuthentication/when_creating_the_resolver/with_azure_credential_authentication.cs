// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Server.for_ConnectionResolverAuthentication.when_creating_the_resolver;

/// <summary>
/// Microsoft Entra ID authentication configured under storage.connectionResolution reaches the HTTP resolver,
/// and the kernel's Azure registration provides its tokens.
/// </summary>
public class with_azure_credential_authentication : given.a_configured_storage_connection_resolution
{
    void Establish() => Configure(new Dictionary<string, string?>
    {
        [$"{Resolution}:Resolver"] = "Http",
        [$"{Resolution}:Http:Endpoint"] = Endpoint,
        [$"{Resolution}:Http:Authentication:Type"] = "AzureCredential",
        [$"{Resolution}:Http:Authentication:Credential"] = "WorkloadIdentity",
        [$"{Resolution}:Http:Authentication:ClientId"] = "resolver-client",
        [$"{Resolution}:Http:Authentication:Scope"] = "api://platform-mgmt/.default"
    });

    void Because() => CreateResolver();

    [Fact] void should_create_the_resolver() => _error.ShouldBeNull();
    [Fact] void should_hand_the_authentication_type_to_the_resolver() => _authenticationReachingTheResolver!.Type.ShouldEqual(HttpConnectionResolverAuthenticationType.AzureCredential);
    [Fact] void should_hand_the_credential_to_the_resolver() => _authenticationReachingTheResolver!.Credential.ShouldEqual(AzureCredentialType.WorkloadIdentity);
    [Fact] void should_hand_the_client_id_to_the_resolver() => _authenticationReachingTheResolver!.ClientId.ShouldEqual("resolver-client");
    [Fact] void should_hand_the_scope_to_the_resolver() => _authenticationReachingTheResolver!.Scope.ShouldEqual("api://platform-mgmt/.default");
}
