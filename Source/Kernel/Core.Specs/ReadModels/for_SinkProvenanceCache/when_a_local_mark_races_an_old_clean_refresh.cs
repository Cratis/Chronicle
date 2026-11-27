// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_a_local_mark_races_an_old_clean_refresh : given.a_cached_database
{
    SinkProvenanceLease _alias;
    Exception _denied;

    async Task Because()
    {
        _alias = await _cache.Bind("different-route", () => _raw).GetLease();
        _raw.Hold = true;
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _cached.MarkQuarantined();
        _raw.Release();
        _denied = Catch.Exception(_alias.Check);
    }

    [Fact] void should_share_one_physical_lease_across_aliases() => ReferenceEquals(_alias, _lease).ShouldBeTrue();
    [Fact] void should_notify_existing_readers_without_payload_events() => _lease.Revoked.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_publish_the_old_clean_result_as_permission() => _denied.ShouldNotBeNull();
}
