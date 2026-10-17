// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;

using KernelEventType = Cratis.Chronicle.Concepts.Events.EventType;
using KernelProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

/// <summary>
/// A kernel projection folds every event by a composite key and may be global. Losing either on the way through
/// storage makes the stored definition differ from the one the kernel declares on every start, which replays the
/// projection from scratch every time.
/// </summary>
public class when_round_tripping_a_kernel_projection_folding_every_event_by_composite_key : Specification
{
    const string Key = "$composite(EventType=$eventContext(EventType.Id), Namespace=$eventContext(Namespace))";

    KernelProjectionDefinition _restored;

    void Because()
    {
        var definition = new KernelProjectionDefinition(
            ProjectionOwner.Kernel,
            EventSequenceId.Log,
            "projection",
            "model",
            true,
            false,
            new(),
            new Dictionary<KernelEventType, FromDefinition>(),
            new Dictionary<KernelEventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false) { Key = Key, AutoMap = AutoMap.Disabled },
            new Dictionary<KernelEventType, RemovedWithDefinition>(),
            new Dictionary<KernelEventType, RemovedWithJoinDefinition>(),
            Scope: ProjectionScope.Global);
        _restored = definition.ToMongoDB().ToKernel();
    }

    [Fact] void should_preserve_the_key_every_event_is_folded_into() => _restored.FromEvery.Key.Value.ShouldEqual(Key);
    [Fact] void should_preserve_the_auto_map_of_the_fold() => _restored.FromEvery.AutoMap.ShouldEqual(AutoMap.Disabled);
    [Fact] void should_preserve_the_scope() => _restored.Scope.ShouldEqual(ProjectionScope.Global);
}
