// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_StoreEntitlementsCache;

public class when_a_refresh_finishes_after_a_newer_publication
{
    [Fact]
    public async Task should_keep_the_revocation_and_its_original_lease()
    {
        var response = new TaskCompletionSource<StoreEntitlementsState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var grain = Substitute.For<IStoreEntitlements>();
        grain.GetSnapshot().Returns(response.Task);
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(grain);
        var clock = new EntitlementClock();
        using var cache = new StoreEntitlementsCache(factory, NullLogger<StoreEntitlementsCache>.Instance, clock);
        var refreshing = cache.Refresh();
        cache.Publish(new(3, []));
        var publishedAt = cache.LoadedAt;
        clock.Advance(TimeSpan.FromMinutes(6));
        response.SetResult(new(2, [new(new(PrincipalKey.LocalIssuer, "app"), "store", null, StoreAccess.Read, EventStoreAccessGrantSource.Command)]));
        await refreshing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        cache.Snapshot.Version.ShouldEqual(3L);
        cache.Snapshot.Allows(new(PrincipalKey.LocalIssuer, "app"), "store", null, StoreAccess.Read).ShouldBeFalse();
        cache.LoadedAt.ShouldEqual(publishedAt);
        cache.IsFresh.ShouldBeFalse();
    }
}
