// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_a_reducer_depends_on_a_registered_service : Specification
{
    ReadModelScenario<BonusTally> _scenario;
    IBonusProvider _bonusProvider;
    readonly EventSourceId _id = EventSourceId.New();

    void Establish()
    {
        _bonusProvider = Substitute.For<IBonusProvider>();
        _bonusProvider.GetBonus().Returns(10);

        _scenario = new ReadModelScenario<BonusTally>();
        _scenario.Services.AddSingleton(_bonusProvider);
    }

    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(new Tallied(), new Tallied());

    [Fact] void should_add_one_plus_the_injected_bonus_per_event() => Assert.Equal(22, _scenario.Instance!.Count);
}
