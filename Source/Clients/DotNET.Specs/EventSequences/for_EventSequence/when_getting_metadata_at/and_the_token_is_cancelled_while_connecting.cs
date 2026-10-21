// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_the_token_is_cancelled_while_connecting : given.all_dependencies
{
    readonly TaskCompletionSource<IServices> _connect = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IChronicleServicesAccessor _accessor;
    EventSequence _eventSequence;
    bool _completedBeforeCancellation;
    Exception _error;

    void Establish()
    {
        var connection = (IChronicleConnection)Substitute.For(
            [typeof(IChronicleConnection), typeof(IChronicleServicesAccessor), typeof(IGetServicesAsynchronously), typeof(IKernelCapabilities)],
            []);
        ((IKernelCapabilities)connection).CapabilitiesAreKnown.Returns(true);
        _accessor = (IChronicleServicesAccessor)connection;
#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
        ((IGetServicesAsynchronously)connection)
            .GetServices(Arg.Any<CancellationToken>())
            .Returns(call => new ValueTask<IServices>(_connect.Task.WaitAsync(call.Arg<CancellationToken>())));
#pragma warning restore CA2012

        _eventSequence = new(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            connection,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default);
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var metadata = _eventSequence.GetMetadataAt([(EventSequenceNumber)42UL], cancellation.Token);
        _completedBeforeCancellation = metadata.IsCompleted;
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => metadata);
    }

    [Fact] void should_return_control_while_the_connection_is_pending() => _completedBeforeCancellation.ShouldBeFalse();
    [Fact] void should_observe_the_token() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_connect_synchronously() => _ = _accessor.DidNotReceive().Services;
}
