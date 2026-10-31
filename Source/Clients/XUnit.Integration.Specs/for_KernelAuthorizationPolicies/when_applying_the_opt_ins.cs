// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_KernelAuthorizationPolicies;

public class when_applying_the_opt_ins : Specification
{
    ServiceProvider _provider;
    string[] _optedIn;

    void Establish()
    {
        var services = new ServiceCollection();
        KernelAuthorizationPolicies.Apply(services);
        _provider = services.BuildServiceProvider();
    }

    void Because() => _optedIn = [.. _provider.GetServices<AnonymousAspNetAuthorizationPolicyRegistration>().Select(_ => _.Name)];

    [Fact] void should_opt_both_policies_into_anonymous_evaluation() => _optedIn.ShouldContainOnly("Chronicle.Administrator", "Chronicle.Administrator.Operations");

    void Destroy() => _provider.Dispose();
}
