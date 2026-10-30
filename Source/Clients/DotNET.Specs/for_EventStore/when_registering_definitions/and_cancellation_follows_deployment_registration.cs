// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_cancellation_follows_deployment_registration : given.an_event_store_with_buildable_definitions
{
    CancellationTokenSource _cancellation;

    void Establish()
    {
        _cancellation = new();
        _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(async _ =>
        {
            await _cancellation.CancelAsync();
            return new Contracts.Commands.CommandResult();
        });
    }

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(cancellationToken: _cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    [Fact]
    void should_propagate_cancellation() => (_failure is OperationCanceledException).ShouldBeTrue();

    [Fact]
    void should_not_send_the_first_wave() => _eventStore.EventTypes.DidNotReceive().Register();
}
