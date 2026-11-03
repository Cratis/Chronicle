// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_service_waiting_is_cancelled : given.an_event_store_with_buildable_definitions
{
    CancellationTokenSource _cancellation;

    void Establish()
    {
        _cancellation = new();
        _servicesAccessor = Substitute.For<IChronicleServicesAccessor, IGetServicesAsynchronously>();
#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
        ((IGetServicesAsynchronously)_servicesAccessor).GetServices(_cancellation.Token).Returns(_ => new ValueTask<IServices>(CancelWhileWaiting()));
#pragma warning restore CA2012
        SetField("_servicesAccessor", _servicesAccessor);
    }

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(cancellationToken: _cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    async Task<IServices> CancelWhileWaiting()
    {
        await _cancellation.CancelAsync();
        return await Task.FromCanceled<IServices>(_cancellation.Token);
    }

    [Fact]
    void should_propagate_cancellation() => (_failure is OperationCanceledException).ShouldBeTrue();

    [Fact]
    async Task should_wait_for_services() => await ((IGetServicesAsynchronously)_servicesAccessor).Received(1).GetServices(_cancellation.Token);

    [Fact]
    void should_not_send_a_deployment() => _services.EventStores.DidNotReceive().RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>());
}
