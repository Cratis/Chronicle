// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_activating_through_a_service_provider : a_borrowed_store
{
    ReadModelScenario<BonusTally> _supplied;
    ReadModelScenario<BonusTally> _registered;
    ServiceProvider _provider;

    void Establish()
    {
        var bonus = Substitute.For<IBonusProvider>();
        bonus.GetBonus().Returns(10);
        _provider = new ServiceCollection().AddSingleton(bonus).BuildServiceProvider();
        _supplied = Borrowed<BonusTally>(_provider);
        _registered = Borrowed<BonusTally>();
        _registered.Services.AddSingleton(bonus);
    }

    async Task Because()
    {
        await _supplied.Given.ForEventSource(new EventSourceId(Guid.NewGuid())).Events(new Tallied(), new Tallied());
        await _registered.Given.ForEventSource(new EventSourceId(Guid.NewGuid())).Events(new Tallied());
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_activate_the_reducer_from_the_supplied_provider() => _supplied.Instance!.Count.ShouldEqual(22);
    [Fact] void should_activate_the_reducer_from_registered_services() => _registered.Instance!.Count.ShouldEqual(11);
}
