// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Registrations.for_RegistrationWaitExtensions.when_waiting_for_registration;

/// <summary>
/// The timeout is for a registration that never finished, not for one that finished badly.
/// </summary>
/// <remarks>
/// A failed run used to leave the outcome unset, so waiting on it was indistinguishable from waiting on one still in
/// flight: the wait ran to its timeout and threw one, and the exception that actually stopped registration was
/// nowhere in what the caller got back.
/// </remarks>
public class and_the_run_failed : Specification
{
    static readonly Exception _failure = new("the kernel refused the call");

    IEventStore _eventStore;
    RegistrationOutcome _result;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty, _failure));
    }

    async Task Because() => _result = await _eventStore.WaitForRegistration(TimeSpan.FromSeconds(30));

    [Fact] void should_return_rather_than_time_out() => _result.ShouldNotBeNull();
    [Fact] void should_carry_what_stopped_the_run() => _result.Failure.ShouldEqual(_failure);
    [Fact] void should_not_report_it_as_successful() => _result.IsSuccess.ShouldBeFalse();
}
