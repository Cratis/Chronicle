// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// Registering is what a host does on its way up, so a registration that fails takes the host with it. A kernel busy
/// catching a new read model up over a large event log answers late rather than wrongly, and a host that dies on that
/// answer comes back and re-registers, adding its whole registration to the queue it was already waiting on. Waiting
/// it out here is what breaks that loop.
/// </summary>
public class and_the_kernel_is_busy_at_first : given.an_event_store_with_a_projection_that_cannot_be_built
{
    static readonly Exception _busy = new("Response did not arrive on time");

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
            return _attempts < 3 ? Task.FromException(_busy) : Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _projections.Discover();
        _thrown = await Catch.Exception(() => _eventStore.RegisterAll());
        _outcome = _eventStore.Registration;
    }

    [Fact] void should_keep_trying_until_the_kernel_answers() => _attempts.ShouldEqual(3);
    [Fact] void should_not_surface_a_failure() => _thrown.ShouldBeNull();
    [Fact] void should_report_registration_as_successful() => _outcome.IsSuccess.ShouldBeTrue();
}
