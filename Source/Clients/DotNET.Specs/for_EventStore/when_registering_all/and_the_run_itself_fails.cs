// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// A registration that failed is still an answer, and the only one a waiting consumer can act on.
/// </summary>
/// <remarks>
/// The outcome used to be published only on the success path, so a failed run left it indistinguishable from one
/// still in flight - every waiter sat out its whole timeout and then reported a timeout instead of the failure that
/// actually happened. Whatever registered before the failure is reported alongside it, because a partial read side
/// is what the consumer is now holding.
/// </remarks>
public class and_the_run_itself_fails : given.an_event_store_with_a_projection_that_cannot_be_built
{
    static readonly Exception _failure = new("the kernel refused the seeding call");

    RegistrationOutcome _outcome;
    Exception _thrown;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(Task.FromException(_failure));
    }

    async Task Because()
    {
        await _projections.Discover();
        _thrown = await Catch.Exception(() => _eventStore.RegisterAll());
        _outcome = _eventStore.Registration;
    }

    [Fact] void should_still_surface_the_failure_to_the_caller() => _thrown.ShouldEqual(_failure);
    [Fact] void should_report_that_registration_ran() => _outcome.HasRun.ShouldBeTrue();
    [Fact] void should_not_report_it_as_successful() => _outcome.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_what_stopped_the_run() => _outcome.Failure.ShouldEqual(_failure);
    [Fact] void should_report_what_registered_before_it_stopped() => _outcome.Artifacts.Select(_ => _.ArtifactType).ShouldContainOnly([typeof(BuildableProjection)]);
}
