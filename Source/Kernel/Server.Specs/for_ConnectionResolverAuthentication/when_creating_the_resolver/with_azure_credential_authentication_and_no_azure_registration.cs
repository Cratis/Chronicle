// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Server.for_ConnectionResolverAuthentication.when_creating_the_resolver;

/// <summary>
/// Without the Azure registration the kernel server adds, nothing can provide Microsoft Entra ID tokens, so the
/// resolver refuses to start rather than calling the endpoint unauthenticated.
/// </summary>
public class with_azure_credential_authentication_and_no_azure_registration : given.a_configured_storage_connection_resolution
{
    void Establish() => Configure(
        new Dictionary<string, string?>
        {
            [$"{Resolution}:Resolver"] = "Http",
            [$"{Resolution}:Http:Endpoint"] = Endpoint,
            [$"{Resolution}:Http:Authentication:Type"] = "AzureCredential",
            [$"{Resolution}:Http:Authentication:Scope"] = "api://platform-mgmt/.default"
        },
        withAzureAuthentication: false);

    void Because() => CreateResolver();

    [Fact] void should_fail_because_the_authentication_is_not_supported() => _error.ShouldBeOfExactType<HttpConnectionResolverAuthenticationNotSupported>();
}
