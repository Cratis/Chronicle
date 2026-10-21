// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.for_EntraAuthentication;

public class when_looking_up_the_authentication_providers : Specification
{
    SqlAuthenticationProvider _managedIdentity;
    SqlAuthenticationProvider _default;
    SqlAuthenticationProvider _workloadIdentity;
    SqlAuthenticationProvider _servicePrincipal;

    void Because()
    {
        // The Azure extension registers its providers when its assembly loads; touching the type does that.
        _ = typeof(Microsoft.Data.SqlClient.ActiveDirectoryAuthenticationProvider);
        _managedIdentity = SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity);
        _default = SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryDefault);
        _workloadIdentity = SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryWorkloadIdentity);
        _servicePrincipal = SqlAuthenticationProvider.GetProvider(SqlAuthenticationMethod.ActiveDirectoryServicePrincipal);
    }

    [Fact] void should_have_a_managed_identity_provider() => _managedIdentity.ShouldNotBeNull();
    [Fact] void should_have_a_default_provider() => _default.ShouldNotBeNull();
    [Fact] void should_have_a_workload_identity_provider() => _workloadIdentity.ShouldNotBeNull();
    [Fact] void should_have_a_service_principal_provider() => _servicePrincipal.ShouldNotBeNull();
}
