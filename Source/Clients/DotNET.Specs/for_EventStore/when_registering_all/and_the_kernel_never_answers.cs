// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// Retrying buys a busy kernel time; it does not turn a broken one into a working one. Once the attempts are spent
/// the failure still reaches the caller, so a host that genuinely cannot register still fails to start rather than
/// coming up half-wired.
/// </summary>
public class and_the_kernel_never_answers : given.an_event_store_with_a_projection_that_cannot_be_built
{
    static readonly Exception _failure = new("the kernel refused the seeding call");

    int _attempts;
    RegistrationOutcome _outcome;
    Exception _thrown;

    void Establish()
    {
        _registrationRetry.MaxAttempts = 3;
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(_ =>
        {
            _attempts++;
            return Task.FromException(_failure);
        });
    }

    async Task Because()
    {
        await _projections.Discover();
        _thrown = await Catch.Exception(() => _eventStore.RegisterAll());
        _outcome = _eventStore.Registration;
    }

    [Fact] void should_stop_at_the_configured_number_of_attempts() => _attempts.ShouldEqual(3);
    [Fact] void should_surface_the_failure_to_the_caller() => _thrown.ShouldEqual(_failure);
    [Fact] void should_not_report_registration_as_successful() => _outcome.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_what_stopped_the_run() => _outcome.Failure.ShouldEqual(_failure);
}
