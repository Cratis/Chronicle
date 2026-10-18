// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class with_legacy_streams_and_conjunction_support : given.a_definition_backed_batch
{
    void Establish() => ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.ConcurrencyScopeConjunctions]);

    async Task Because() => await Append([
        new(_sourceId, "first") { EventStreamType = "stream", EventStreamId = "first" },
        new(_sourceId, "second") { EventStreamType = "stream", EventStreamId = "second" }
    ]);

    [Fact] void should_not_refuse_the_batch() => _exception.ShouldBeNull();
    [Fact] void should_send_every_stream_guard() => _request.ConcurrencyScopes.Count().ShouldEqual(2);
}
