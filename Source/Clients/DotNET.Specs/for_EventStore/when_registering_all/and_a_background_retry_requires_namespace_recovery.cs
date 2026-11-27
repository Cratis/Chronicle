// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_a_background_retry_requires_namespace_recovery : given.an_event_store_with_a_projection_that_cannot_be_built
{
    int _attempts;

    void Establish()
    {
        _registrationRetry.MaxAttempts = 1;
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(_ =>
        {
            _attempts++;
            return _attempts switch
            {
                1 => Task.FromException(new InvalidOperationException("Temporary registration failure")),
                2 => Task.FromException(new TargetInvocationException(new CommandFailed(new() { ExceptionMessages = ["NamespaceRecoveryRequired"] }))),
                _ => Task.CompletedTask
            };
        });
    }

    async Task Because()
    {
        await _projections.Discover();
        await Catch.Exception(_eventStore.RegisterAll);
        await _eventStore.PendingBackgroundRegistrationRetry!;
    }

    [Fact] void should_stop_the_background_loop_on_the_terminal_code() => _attempts.ShouldEqual(2);
    [Fact] void should_not_publish_success_after_the_terminal_failure() => _eventStore.Registration.IsSuccess.ShouldBeFalse();
}
