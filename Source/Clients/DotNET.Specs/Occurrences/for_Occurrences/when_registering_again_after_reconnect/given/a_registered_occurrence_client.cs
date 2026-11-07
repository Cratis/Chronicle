// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_registering_again_after_reconnect.given;

public class a_registered_occurrence_client : an_occurrence_client
{
    protected readonly TaskCompletionSource _observedAgain = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected int _observations;

    protected virtual bool RegisterAgainAfterReconnect => true;

    async Task Establish()
    {
        _artifacts.OccurrenceHandlers.Returns([typeof(TestOccurrenceHandler)]);
        _occurrences.Configure(_artifacts, _schemas, new JsonSerializerOptions(JsonSerializerDefaults.Web), _services, _activator, _causation, _identity, NullLogger<Occurrences>.Instance, registerAgainAfterReconnect: RegisterAgainAfterReconnect);
        _service.ObserveOccurrences(Arg.Any<ObserveOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref _observations) == 1) _observed.TrySetResult();
            else _observedAgain.TrySetResult();
            return _notifications;
        });
        await _lifecycle.Connected();
        await _occurrences.Discover();
        await _occurrences.Register();
        await _observed.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
    }
}
