// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_a_remote_silo_marks_the_database : given.a_cached_database
{
    bool _revoked;

    async Task Because()
    {
        using var other = new SinkProvenanceCache(_clock);
        var remote = other.Bind("remote-route", () => _raw);
        await remote.GetLease();
        await remote.MarkQuarantined();
        _clock.Advance(TimeSpan.FromSeconds(1));
        _revoked = _lease.Revoked.IsCancellationRequested;
    }

    [Fact] void should_revoke_an_independent_silos_lease_on_refresh() => _revoked.ShouldBeTrue();
}
