// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_no_host_registers_the_policies : given.the_kernel_authorization
{
    void Establish()
    {
        _services.AddChronicleAdministratorAuthorization();
        BuildProvider();
    }

    [Fact] void should_add_the_administrator_policy_with_the_kernel_requirement() => HasRequirement<AdministratorRequirement>(Options.GetPolicy(WellKnownAuthorizationPolicies.Administrator)).ShouldBeTrue();
    [Fact] void should_add_the_operations_policy_with_the_kernel_requirement() => HasRequirement<SystemStoreOperationRequirement>(Options.GetPolicy(WellKnownAuthorizationPolicies.AdministratorOperations)).ShouldBeTrue();
    [Fact] void should_contain_only_the_kernel_requirement_in_the_administrator_policy() => Options.GetPolicy(WellKnownAuthorizationPolicies.Administrator)!.Requirements.Count.ShouldEqual(1);
    [Fact] void should_not_set_a_fallback_policy() => Options.FallbackPolicy.ShouldBeNull();
    [Fact] void should_resolve_the_administrator_authorization() => _services.Any(_ => _.ServiceType == typeof(IAdministratorAuthorization)).ShouldBeTrue();
}
