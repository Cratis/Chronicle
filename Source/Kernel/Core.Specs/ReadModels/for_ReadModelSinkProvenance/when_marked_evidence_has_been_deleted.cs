// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_marked_evidence_has_been_deleted : given.a_provenance_inventory
{
    Exception? _error;
    bool _restricted;

    async Task Because()
    {
        _projections = [_projection with { EventSequenceId = "system" }];
        await _provenance.Preserve(Store);
        _models = [];
        _projections = [];
        _history = [];
        var restarted = new ReadModelSinkProvenance(_storage);
        _error = await Catch.Exception(() => restarted.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        _restricted = await Marker(EventStoreNamespaceName.Default).IsQuarantined();
    }

    [Fact] void should_refuse_a_new_coordinator_without_any_remaining_definitions() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_clear_the_durable_database_marker() => _restricted.ShouldBeTrue();
}
