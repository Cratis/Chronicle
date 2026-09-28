// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections;

public class when_forgetting_runtime_registrations : given.projections_for_explicit_registration
{
    async Task Establish()
    {
        _projections.Add(new DeclarativeExplicitProjection<Customer>(projection => projection.From<CustomerRegistered>(), "from-options"));
        await _projections.Discover();
        await _projections.Register<Inventory>(projection => projection.From<ItemAdded>());
    }

    void Because() => _projections.ForgetRuntimeRegistrations();

    [Fact] void should_forget_the_read_model_registered_at_runtime() => _projections.HasFor<Inventory>().ShouldBeFalse();
    [Fact] void should_keep_the_read_model_from_the_options() => _projections.HasFor<Customer>().ShouldBeTrue();
    [Fact] void should_only_declare_the_one_from_the_options() => _projections.Definitions.Single().Identifier.ShouldEqual("from-options");
}
