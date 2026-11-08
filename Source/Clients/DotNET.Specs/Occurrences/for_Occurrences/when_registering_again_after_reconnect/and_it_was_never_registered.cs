// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_registering_again_after_reconnect;

public class and_it_was_never_registered : an_occurrence_client
{
    void Establish() => _occurrences.Configure(_artifacts, _schemas, new JsonSerializerOptions(JsonSerializerDefaults.Web), _services, _activator, _causation, _identity, NullLogger<Occurrences>.Instance, registerAgainAfterReconnect: true);

    async Task Because()
    {
        await _lifecycle.Connected();
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
    }

    [Fact] void should_not_register_triggers() => _service.DidNotReceive().RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_observe_occurrences() => _service.DidNotReceive().ObserveOccurrences(Arg.Any<ObserveOccurrencesRequest>(), Arg.Any<CallContext>());
}
