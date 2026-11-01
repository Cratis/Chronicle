// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.Serialization.for_ObserverSubscriptionJsonConverter;

public class when_round_tripping_controlled_targets : Specification
{
    static readonly EventType _first = new("529bd8fb-d0ce-4d40-992b-205ee551f217", 2);
    static readonly EventType _second = new("758264df-adc9-4745-9b3c-85ea6920b2df", 1);
    ObserverSubscription _result;

    void Because()
    {
        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(new ServiceCollection());
        silo.ConfigureSerialization();
        using var services = silo.Services.BuildServiceProvider();
        var options = services.GetRequiredService<JsonSerializerOptions>();
        var original = ObserverSubscription.Unsubscribed with
        {
            TypeSetRevision = new(3),
            TypeSetMembers = new Dictionary<EventTypeId, ObserverTypeSetMember>
            {
                [_first.Id] = new(new(1), null),
                [_second.Id] = new(new(3), 20)
            },
            Targets =
            [
                new(SiloAddress.Zero) { DeclaredEventTypes = [_first], GenerationDelivery = EventGenerationDelivery.Compatibility },
                new(SiloAddress.Zero) { DeclaredEventTypes = [_first, _second], GenerationDelivery = EventGenerationDelivery.Pinned }
            ]
        };
        _result = JsonSerializer.Deserialize<ObserverSubscription>(JsonSerializer.Serialize(original, options), options)!;
    }

    [Fact] void should_keep_the_revision() => _result.TypeSetRevision.ShouldEqual(new ObserverTypeSetRevision(3));
    [Fact] void should_keep_both_members() => _result.TypeSetMembers!.Count.ShouldEqual(2);
    [Fact] void should_keep_the_live_join_fence() => _result.TypeSetMembers![_second.Id].JoinFence.ShouldEqual((EventSequenceNumber)20);
    [Fact] void should_keep_the_unfenced_member() => _result.TypeSetMembers![_first.Id].JoinFence.ShouldBeNull();
    [Fact] void should_keep_both_targets() => _result.Targets.Count.ShouldEqual(2);
    [Fact] void should_keep_the_first_targets_pins() => _result.Targets[0].DeclaredEventTypes!.Single().ShouldEqual(_first);
    [Fact] void should_keep_the_second_targets_capabilities() => _result.Targets[1].DeclaredEventTypes!.Count.ShouldEqual(2);
    [Fact] void should_keep_each_targets_delivery_policy() => _result.Targets[1].GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
}
