// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_for_known_type;

public class and_the_index_definition_changes : Specification
{
    ISink _first;
    ISink _changed;
    ISink _result;
    ICanReconcileIndexes _reconciler;
    Sinks _sinks;
    ReadModelDefinition _definition;

    void Establish()
    {
        SinkTypeId type = "df371e5d-b244-48d0-aaad-f298a127dd92";
        _definition = new ReadModelDefinition("model", "model", "Model", ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, new SinkDefinition(SinkConfigurationId.None, type), new Dictionary<ReadModelGeneration, JsonSchema>(), [new IndexDefinition("first")]);
        _first = Substitute.For<ISink>();
        _changed = Substitute.For<ISink, ICanReconcileIndexes>();
        _reconciler = (ICanReconcileIndexes)_changed;
        var factory = Substitute.For<ISinkFactory>();
        factory.TypeId.Returns(type);
        factory.CreateFor(string.Empty, string.Empty, Arg.Any<ReadModelDefinition>()).Returns(_first, _changed);
        _sinks = new Sinks(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([factory]));
    }

    async Task Because()
    {
        await _sinks.GetFor(_definition);
        var definition = _definition with { Indexes = [new IndexDefinition("second")] };
        await _sinks.GetFor(definition);
        _result = await _sinks.GetFor(definition with { Indexes = [new IndexDefinition("second")] });
    }

    [Fact] void should_ensure_without_pruning_on_first_activation() => _first.Received(1).EnsureIndexes();
    [Fact] void should_reconcile_only_once_after_an_actual_definition_change() => _reconciler.Received(1).ReconcileIndexes();
    [Fact] void should_not_use_routine_ensure_for_reconciliation() => _changed.DidNotReceive().EnsureIndexes();
    [Fact] void should_cache_the_changed_sink() => _result.ShouldEqual(_changed);
}
