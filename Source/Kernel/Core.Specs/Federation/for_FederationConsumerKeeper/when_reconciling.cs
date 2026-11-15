// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Federation.for_FederationConsumerKeeper;

public class when_reconciling : given.a_link_pump
{
    IGrainFactory _grains;
    IFederationConsumer _first;
    IFederationConsumer _broken;
    IFederationConsumer _removed;
    FederationConsumerKeeper _keeper;
    List<FederationBindingState> _bindings;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _first = Substitute.For<IFederationConsumer>();
        _broken = Substitute.For<IFederationConsumer>();
        _broken.Ensure().Returns(Task.FromException(new InvalidOperationException("unreachable")));
        _removed = Substitute.For<IFederationConsumer>();
        _grains.GetGrain<IFederationConsumer>("first", null).Returns(_first);
        _grains.GetGrain<IFederationConsumer>("broken", null).Returns(_broken);
        _grains.GetGrain<IFederationConsumer>("removed", null).Returns(_removed);

        _bindings = [CreateBinding("first"), CreateBinding("broken"), CreateBinding("removed")];
        _registry.GetBindings().Returns(_ => Task.FromResult<IReadOnlyList<FederationBindingState>>([.. _bindings]));
        _keeper = new(_grains, NullLogger<FederationConsumerKeeper>.Instance, _options, _registry);
    }

    async Task Because()
    {
        await _keeper.Reconcile();
        _bindings.RemoveAll(_ => _.Id == "removed");
        await _keeper.Reconcile();
    }

    [Fact] void should_ensure_a_consumer_for_every_binding() => _first.Received(2).Ensure();
    [Fact] void should_keep_going_when_one_consumer_cannot_be_reached() => _first.Received().Ensure();
    [Fact] void should_stop_the_consumer_of_a_removed_binding() => _removed.Received(1).Stop();
    [Fact] void should_not_stop_consumers_of_remaining_bindings() => _first.DidNotReceive().Stop();
}
