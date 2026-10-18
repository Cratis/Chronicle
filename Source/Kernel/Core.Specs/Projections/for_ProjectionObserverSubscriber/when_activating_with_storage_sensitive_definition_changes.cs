// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_activating_with_storage_sensitive_definition_changes : given.a_subscriber_with_a_cached_pipeline
{
    [Theory]
    [InlineData("legacy")]
    [InlineData("scope")]
    [InlineData("key")]
    [InlineData("automap")]
    [InlineData("child-key")]
    [InlineData("child-automap")]
    [InlineData("nested-key")]
    [InlineData("nested-automap")]
    async Task should_adopt_the_authoritative_definition_before_building_the_pipeline(string change)
    {
        var previous = _definition;
        var current = _definition;
        if (change == "legacy")
        {
            previous = previous with { HasLegacyStorageFields = true };
        }
        else if (change == "scope")
        {
            current = current with { Scope = ProjectionScope.Global };
        }
        else
        {
            var every = change.EndsWith("automap", StringComparison.Ordinal)
                ? current.FromEvery with { AutoMap = AutoMap.Disabled }
                : current.FromEvery with { Key = "CompositeKey" };
            if (change.StartsWith("child-", StringComparison.Ordinal))
            {
                previous = previous with { Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = Child(previous.FromEvery) } };
                current = current with { Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = Child(every) } };
            }
            else if (change.StartsWith("nested-", StringComparison.Ordinal))
            {
                previous = previous with { Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = Child(previous.FromEvery) } };
                current = current with { Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = Child(every) } };
            }
            else
            {
                current = current with { FromEvery = every };
            }
        }
        _stateStorage.State = previous;
        _projectionGrain.GetDefinition().Returns(current);

        await _subscriber.OnActivateAsync(CancellationToken.None);

        _stateStorage.State.ShouldBeSame(previous);
        await _stateStorage.DidNotReceive().WriteStateAsync();
        _factory.ReceivedCalls().Select(call => call.GetArguments()[2]).ShouldContain(current);
    }

    static ChildrenDefinition Child(FromEveryDefinition every) => new(
        "id",
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        every,
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());
}
