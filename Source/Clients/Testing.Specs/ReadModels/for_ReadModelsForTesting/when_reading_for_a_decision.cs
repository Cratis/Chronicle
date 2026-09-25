// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting;

public class when_reading_for_a_decision
{
    readonly EventType _created = new("Created", EventTypeGeneration.First);
    readonly EventType _removed = new("Removed", EventTypeGeneration.First);

    [Fact]
    public async Task should_return_only_an_explicitly_seeded_guarded_read()
    {
        var wrapper = new ReadModelsForTesting(Substitute.For<IReadModels>());
        wrapper.RegisterDecisionInstance(new ReadModelInstance<string>("source", null, 9, [_created, _removed]));
        var read = await ((IReadModels)wrapper).GetInstanceForDecision<string>("source");
        read.Instance.ShouldBeNull();
        read.ToConcurrencyScope().SequenceNumber.ShouldEqual((EventSequenceNumber)9);
        read.ToConcurrencyScope().EventTypes.ShouldContain(_removed);
    }

    [Fact]
    public async Task should_refuse_to_invent_a_watermark_for_an_unseeded_key()
    {
        var wrapper = new ReadModelsForTesting(Substitute.For<IReadModels>());
        await Assert.ThrowsAsync<NotSupportedException>(() => ((IReadModels)wrapper).GetInstanceForDecision<string>("source"));
    }
}
