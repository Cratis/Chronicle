// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_an_application_registers_a_same_named_policy_without_the_requirement : given.the_kernel_authorization
{
    Exception _result;

    void Establish()
    {
        _services.AddAuthorizationBuilder().AddPolicy(WellKnownAuthorizationPolicies.Administrator, _ => _.RequireAuthenticatedUser());
        _services.AddChronicleAdministratorAuthorization();
        BuildProvider();
    }

    void Because() => _result = Catch.Exception(() => _ = Options);

    [Fact] void should_reject_the_reserved_name() => _result.ShouldBeOfExactType<KernelAuthorizationPolicyNameIsReserved>();
}
