// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_activated;

public class and_persisted_definitions_use_an_unsupported_sequence : given.a_projections_manager_grain
{
    void Establish()
    {
        var definitions = from owner in Enum.GetValues<ProjectionOwner>().Where(_ => _ != ProjectionOwner.Kernel)
                          from sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" }
                          select CreateDefinition(Guid.NewGuid().ToString(), "the-read-model") with { Owner = owner, EventSequenceId = sequence };
        _state.Projections = [.. definitions];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.ClearReceivedCalls();
        _projectionFactory.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
        await _grain.EnsureSubscribedIn("new-namespace");
    }

    [Fact] void should_not_compile_a_definition() => _projectionFactory.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_subscribe_an_observer() => _observerGrain.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Subscribe").ShouldBeFalse();
    [Fact] void should_not_subscribe_an_alternate_observer() => _observersByKey.Values.SelectMany(_ => _.ReceivedCalls()).Any(_ => _.GetMethodInfo().Name == "Subscribe").ShouldBeFalse();
    [Fact] void should_leave_the_persisted_definitions_in_place() => _state.Projections.Any().ShouldBeTrue();
}
