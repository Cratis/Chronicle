// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSources;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Registrations.for_ArtifactFingerprintCalculator.given;

public class a_complete_set_of_definitions : Specification
{
    private protected static ArtifactDefinitionPayloads Build() => Build(false);

    private protected static ArtifactDefinitionPayloads Reordered() => Build(true);

    static ArtifactDefinitionPayloads Build(bool reverse) => new(
        Order<Contracts.Events.EventTypeRegistration>(
            [
                new()
                {
                    Type = new() { Id = "first-event", Generation = 2 },
                    Schema = "{\"type\":\"object\"}",
                    EventStore = "first-store",
                    Generations = [new() { Generation = 1, Schema = "{}" }, new() { Generation = 2, Schema = "{}" }]
                },
                new() { Type = new() { Id = "second-event", Generation = 1 }, Schema = "{}", EventStore = "first-store" }
            ],
            reverse),
        Order<EventSourceDefinition>(
            [
                new() { Name = "first-source", Description = "First source" },
                new() { Name = "second-source", Description = "Second source" }
            ],
            reverse),
        Order<ReadModelDefinition>(
            [
                new() { Type = new() { Identifier = "first-model", Generation = 1 }, Schema = "{}", Sink = new() },
                new() { Type = new() { Identifier = "second-model", Generation = 1 }, Schema = "{}", Sink = new() }
            ],
            reverse),
        Order<Constraint>(
            [
                new() { Name = "first-constraint", Definition = new() { Value0 = new UniqueConstraintDefinition { IgnoreCasing = true } } },
                new() { Name = "second-constraint", Definition = new() { Value0 = new UniqueConstraintDefinition() } }
            ],
            reverse),
        Order<ProjectionDefinition>([Projection("first-projection", reverse), Projection("second-projection", reverse)], reverse));

    static ProjectionDefinition Projection(string identifier, bool reverse) => new()
    {
        Identifier = identifier,
        ReadModel = "first-model",
        IsActive = true,
        LastUpdated = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        From = Events(() => new FromDefinition { Key = "id", Properties = Properties(reverse) }, reverse),
        Join = Events(() => new JoinDefinition { On = "id", Key = "id", Properties = Properties(reverse) }, reverse),
        All = new() { Properties = Properties(reverse) },
        FromEvery = [new() { EventTypes = [new() { Id = "first-event", Generation = 1 }], From = new() { Key = "id", Properties = Properties(reverse) } }],
        Children = Children(reverse, 1),
        Nested = Children(reverse, 1),
        RemovedWith = Events(() => new RemovedWithDefinition { Key = "id" }, reverse),
        RemovedWithJoin = Events(() => new RemovedWithJoinDefinition { Key = "id" }, reverse)
    };

    static Dictionary<string, ChildrenDefinition> Children(bool reverse, int depth) =>
        Order<string>(["first-child", "second-child"], reverse).ToDictionary(key => key, _ => new ChildrenDefinition
        {
            IdentifiedBy = "id",
            From = Events(() => new FromDefinition { Key = "id", Properties = Properties(reverse) }, reverse),
            Join = Events(() => new JoinDefinition { On = "id", Key = "id", Properties = Properties(reverse) }, reverse),
            All = new() { Properties = Properties(reverse) },
            RemovedWith = Events(() => new RemovedWithDefinition { Key = "id" }, reverse),
            RemovedWithJoin = Events(() => new RemovedWithJoinDefinition { Key = "id" }, reverse),
            Children = depth > 0 ? Children(reverse, depth - 1) : [],
            Nested = depth > 0 ? Children(reverse, depth - 1) : []
        });

    static Dictionary<EventType, TValue> Events<TValue>(Func<TValue> value, bool reverse) =>
        Order<EventType>([new() { Id = "first-event", Generation = 1 }, new() { Id = "second-event", Generation = 1 }], reverse)
            .ToDictionary(key => key, _ => value());

    static Dictionary<string, string> Properties(bool reverse) =>
        Order<string>(["first", "second"], reverse).ToDictionary(key => key, key => $"event.{key}");

    static T[] Order<T>(T[] items, bool reverse) => reverse ? items.Reverse().ToArray() : items;
}
