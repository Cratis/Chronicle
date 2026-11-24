// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_the_system_sequence_is_previewed : given.a_header_event_preview
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _service.Preview(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "system",
        Declaration = "projection PreviewHeaders"
    }));

    [Fact] void should_refuse_the_preview() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_process_any_events() => _previewed.ShouldBeNull();
}
