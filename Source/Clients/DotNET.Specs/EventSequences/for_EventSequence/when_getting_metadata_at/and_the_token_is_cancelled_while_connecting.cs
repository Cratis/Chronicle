// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_the_token_is_cancelled_while_connecting : given.all_dependencies
{
    const int CancelAfterMilliseconds = 50;

    EventSequence _eventSequence;
    Exception _error;
    TimeSpan _elapsed;

    void Establish()
    {
        var connection = (IChronicleConnection)Substitute.For(
            [typeof(IChronicleConnection), typeof(IChronicleServicesAccessor), typeof(IGetServicesAsynchronously), typeof(IKernelCapabilities)],
            []);
        ((IKernelCapabilities)connection).CapabilitiesAreKnown.Returns(true);
#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
        ((IGetServicesAsynchronously)connection)
            .GetServices(Arg.Any<CancellationToken>())
            .Returns(call => NeverConnects(call.Arg<CancellationToken>()));
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

    static async ValueTask<IServices> NeverConnects(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return null!;
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource(CancelAfterMilliseconds);
        var stopwatch = Stopwatch.StartNew();
        _error = await Catch.Exception(() => _eventSequence.GetMetadataAt([(EventSequenceNumber)42UL], cancellation.Token));
        stopwatch.Stop();
        _elapsed = stopwatch.Elapsed;
    }

    [Fact] void should_observe_the_token() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_cancel_promptly() => _elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
}
