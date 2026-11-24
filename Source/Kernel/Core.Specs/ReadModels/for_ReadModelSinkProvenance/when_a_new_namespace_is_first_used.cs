// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_a_new_namespace_is_first_used : given.a_provenance_inventory
{
    readonly SinkConfigurationId _configuration = Guid.NewGuid();
    Exception? _error;
    bool _marked;

    async Task Because()
    {
        await _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model);
        _projections = [_projection with { EventSequenceId = "system" }];
        _models = [_model with { Sink = _model.Sink with { Configuration = _configuration } }];
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, "new-namespace", _models[0]));
        _marked = await Marker("new-namespace", _configuration).IsQuarantined();
    }

    [Fact] void should_inventory_an_unseen_namespace_and_destination() => _marked.ShouldBeTrue();
    [Fact] void should_refuse_its_first_read() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
