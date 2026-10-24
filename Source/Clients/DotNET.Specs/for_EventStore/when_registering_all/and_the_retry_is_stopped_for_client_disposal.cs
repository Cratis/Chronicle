// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// Disposing the client stops the retry of each store it created; disposing the connection raises no disconnect (#5053).
/// </summary>
public class and_the_retry_is_stopped_for_client_disposal : given.an_event_store_with_a_projection_that_cannot_be_built
{
    bool _retryEnded;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(_ => Task.FromException(new Exception("the kernel rejected the registration")));

        // A long delay parks the loop between attempts, as for a permanently rejected registration. Only the
        // cancellation from detaching can end it; no wait here depends on the delay elapsing.
        SetField("_registrationBackoff", new RegistrationBackoff(TimeSpan.FromHours(1), TimeSpan.FromHours(1)));
    }

    async Task Because()
    {
        await _projections.Discover();
        await Catch.Exception(_eventStore.RegisterAll);
        var retry = _eventStore.PendingBackgroundRegistrationRetry!;

        _eventStore.StopBackgroundRegistrationRetry();

        // A loop that honors cancellation ends at once; a regression leaves this awaiting until the run is aborted.
        await retry;
        _retryEnded = retry.IsCompleted;
    }

    [Fact] void should_end_the_background_retry() => _retryEnded.ShouldBeTrue();
}
