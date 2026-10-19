// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// A store whose creation failed is detached by the client and then dropped. The background registration retry it
/// started must stand down at that point, otherwise the loop - and the connection's disconnect subscription it
/// holds - keeps the whole event store graph alive for as long as the connection lives (#5053).
/// </summary>
public class and_a_failed_store_is_detached_while_retrying_in_the_background : given.an_event_store_with_a_projection_that_cannot_be_built
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
        await Catch.Exception(() => _eventStore.RegisterAll());
        var retry = _eventStore.PendingBackgroundRegistrationRetry!;

        _eventStore.DetachAutomaticRegistration();

        // A loop that honors cancellation ends at once; a regression leaves this awaiting until the run is aborted.
        await retry;
        _retryEnded = retry.IsCompleted;
    }

    [Fact] void should_end_the_background_retry() => _retryEnded.ShouldBeTrue();
}
