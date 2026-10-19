// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

/// <summary>
/// A store whose creation failed is detached by the client and then dropped. The background registration retry it
/// started must stand down at that point, otherwise the loop - and the connection's disconnect subscription it
/// holds - keeps the whole event store graph alive for as long as the connection lives (#5053).
/// </summary>
public class and_a_failed_store_is_detached_while_retrying_in_the_background : given.an_event_store_with_a_projection_that_cannot_be_built
{
    WeakReference _eventStoreReference;
    Task _retry;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _eventStore.Seeding.Register().Returns(_ => Task.FromException(new Exception("the kernel rejected the registration")));

        // A long delay keeps the loop parked between attempts, as it is for a permanently rejected registration.
        SetField("_registrationBackoff", new RegistrationBackoff(TimeSpan.FromHours(1), TimeSpan.FromHours(1)));
    }

    async Task Because()
    {
        await _projections.Discover();
        await Catch.Exception(() => _eventStore.RegisterAll());
        _retry = _eventStore.PendingBackgroundRegistrationRetry!;
        _eventStore.DetachAutomaticRegistration();
        await _retry.WaitAsync(TimeSpan.FromSeconds(10));

        SetField("_backgroundRegistrationRetryTask", null!);
        _eventStoreReference = new WeakReference(_eventStore);
        _eventStore = null!;
        _retry = null!;
        ForceCollection();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ForceCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact] void should_release_the_event_store() => _eventStoreReference.IsAlive.ShouldBeFalse();
}
