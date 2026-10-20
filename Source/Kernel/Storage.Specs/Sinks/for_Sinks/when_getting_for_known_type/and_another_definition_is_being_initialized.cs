// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_for_known_type;

public class and_another_definition_is_being_initialized : Specification
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ISink _original;
    ISink _result;
    bool _completedWithoutWaiting;

    async Task Because()
    {
        SinkTypeId type = "df371e5d-b244-48d0-aaad-f298a127dd92";
        var definition = new ReadModelDefinition("model", "model", "Model", ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, new SinkDefinition(SinkConfigurationId.None, type), new Dictionary<ReadModelGeneration, JsonSchema>(), [new IndexDefinition("first")]);
        _original = Substitute.For<ISink>();
        var changed = Substitute.For<ISink, ICanReconcileIndexes>();
        ((ICanReconcileIndexes)changed).ReconcileIndexes().Returns(_release.Task);
        var factory = Substitute.For<ISinkFactory>();
        factory.TypeId.Returns(type);
        factory.CreateFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>()).Returns(_original, changed);
        var sinks = new Sinks(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([factory]));
        await sinks.GetFor(definition);
        var pending = sinks.GetFor(definition with { Indexes = [new IndexDefinition("second")] });
        var cached = sinks.GetFor(definition);
        _completedWithoutWaiting = cached.IsCompletedSuccessfully;
        _release.TrySetResult();
        _result = await cached;
        await pending;
    }

    [Fact] void should_not_wait_on_the_in_progress_initialization_gate() => _completedWithoutWaiting.ShouldBeTrue();
    [Fact] void should_return_the_already_initialized_sink() => _result.ShouldEqual(_original);
}
