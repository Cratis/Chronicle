// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_the_store_inventory_marker_cannot_be_read : given.a_provenance_inventory
{
    Exception? _error;
    bool _databaseCompleted;

    void Establish() => _eventStore.IsSinkProvenanceInventoryComplete().Returns(_ => Task.FromException<bool>(new InvalidOperationException("unavailable")));

    async Task Because()
    {
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        _databaseCompleted = (await Task.WhenAll(_markers.Values.Select(_ => _.ReadState()))).Any(_ => _.IsComplete);
    }

    [Fact] void should_refuse_the_first_read() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_use_the_neutral_refusal() => ((RpcException)_error!).Status.Detail.ShouldEqual(SystemStoreAccess.Refused);
    [Fact] void should_not_complete_any_database_inventory() => _databaseCompleted.ShouldBeFalse();
    [Fact] void should_not_publish_store_completion() => _inventoryComplete.ShouldBeFalse();
}
