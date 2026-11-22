// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_namespace_recovery_is_required : given.an_event_store_with_a_projection_that_cannot_be_built
{
    readonly Exception _failure = new CommandFailed(new() { ExceptionMessages = ["NamespaceRecoveryRequired"] });
    int _attempts;
    Exception _thrown;

    void Establish()
    {
        _registrationRetry.MaxAttempts = 3;
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(_ =>
        {
            _attempts++;
            return _attempts == 1 ? Task.FromException(_failure) : Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _projections.Discover();
        _thrown = await Catch.Exception(_eventStore.RegisterAll);
    }

    [Fact] void should_surface_the_terminal_failure() => _thrown.ShouldEqual(_failure);
    [Fact] void should_not_spend_the_foreground_retry_budget() => _attempts.ShouldEqual(1);
    [Fact] void should_not_schedule_a_background_retry() => _eventStore.PendingBackgroundRegistrationRetry.ShouldBeNull();
    [Fact] void should_report_failed_registration() => _eventStore.Registration.IsSuccess.ShouldBeFalse();
}
