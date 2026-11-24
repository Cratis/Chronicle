// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Projections;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_inventory_finds_restricted_producers : given.a_provenance_inventory
{
    readonly SinkConfigurationId _historyConfiguration = Guid.NewGuid();
    readonly SinkConfigurationId _missingConfiguration = Guid.NewGuid();
    readonly SinkConfigurationId _kernelConfiguration = Guid.NewGuid();
    Exception? _error;
    bool _stored;
    bool _historical;
    bool _missing;
    bool _global;
    bool _kernel;

    void Establish()
    {
        _projection = _projection with { EventSequenceId = "system", Scope = ProjectionScope.Global };
        var historyModel = _model with { Identifier = "history", ObserverIdentifier = "historical", Sink = _model.Sink with { Configuration = _historyConfiguration } };
        var missingModel = _model with { Identifier = "missing", ObserverIdentifier = "deleted", Sink = _model.Sink with { Configuration = _missingConfiguration } };
        var kernelModel = _model with { Identifier = "kernel", ObserverIdentifier = "kernel", Sink = _model.Sink with { Configuration = _kernelConfiguration } };
        _models = [_model, historyModel, missingModel, kernelModel];
        _projections = [_projection, _projection with { Identifier = "historical", ReadModel = "history", EventSequenceId = "event-log" }, _projection with { Identifier = "kernel", ReadModel = "kernel", Owner = ProjectionOwner.Kernel }];
        _history = [new ProjectionDefinitionHistoryEntry("historical", "digest", "sha256", null, DateTimeOffset.UnixEpoch, _projection with { Identifier = "historical", ReadModel = "history" })];
    }

    async Task Because()
    {
        await _provenance.Preserve(Store);
        _stored = await Marker(EventStoreNamespaceName.Default).IsQuarantined();
        _historical = await Marker(EventStoreNamespaceName.Default, _historyConfiguration).IsQuarantined();
        _missing = await Marker(EventStoreNamespaceName.Default, _missingConfiguration).IsQuarantined();
        _global = await Marker(EventStoreNamespaceName.NotSet).IsQuarantined();
        _kernel = await Marker(EventStoreNamespaceName.Default, _kernelConfiguration).IsQuarantined();
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model with { Identifier = "ordinary", ObserverType = ReadModelObserverType.NotSet, ObserverIdentifier = ReadModelObserverIdentifier.Unspecified }));
    }

    [Fact] void should_mark_the_stored_destination() => _stored.ShouldBeTrue();
    [Fact] void should_mark_the_historical_destination() => _historical.ShouldBeTrue();
    [Fact] void should_mark_a_binding_whose_producer_is_missing() => _missing.ShouldBeTrue();
    [Fact] void should_mark_the_actual_global_destination() => _global.ShouldBeTrue();
    [Fact] void should_not_treat_a_kernel_projection_as_evidence() => _kernel.ShouldBeFalse();
    [Fact] void should_refuse_an_ordinary_model_in_the_same_database() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
