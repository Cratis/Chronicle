// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_StoreEntitlementsCache;

public class when_refreshing_a_snapshot : Specification
{
    IStoreEntitlements _grain;
    StoreEntitlementsCache _first;
    StoreEntitlementsCache _second;
    ManualTime _time;

    void Establish()
    {
        _time = new();
        _grain = Substitute.For<IStoreEntitlements>();
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(_grain);
        _grain.GetSnapshot().Returns(Task.FromResult(new StoreEntitlementsState(1, [])));
        _grain.GetVersion().Returns(Task.FromResult(1L));
        _first = new(grains, NullLogger<StoreEntitlementsCache>.Instance, _time);
        _second = new(grains, NullLogger<StoreEntitlementsCache>.Instance, _time);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task should_load_and_notify_on_startup(long version)
    {
        _grain.GetSnapshot().Returns(Task.FromResult(new StoreEntitlementsState(version, [])));
        var notified = false;
        using var subscription = _first.Changes.Subscribe(_ => notified = true);
        await _first.Load();
        _first.Snapshot.Version.ShouldEqual(version);
        notified.ShouldBeTrue();
        _first.IsFresh.ShouldBeTrue();
        _first.Dispose();
    }

    [Fact]
    async Task should_refresh_both_silos_from_acknowledged_state()
    {
        await _first.Refresh();
        await _second.Refresh();
        var updated = new StoreEntitlementsState(2, [new(new(PrincipalKey.LocalIssuer, "app"), "shop", null, StoreAccess.Read, EventStoreAccessGrantSource.Command)]);
        _first.Publish(updated);
        _second.Publish(updated);
        _first.Snapshot.Allows(new(PrincipalKey.LocalIssuer, "app"), "shop", null, StoreAccess.Read).ShouldBeTrue();
        _second.Snapshot.Allows(new(PrincipalKey.LocalIssuer, "app"), "shop", null, StoreAccess.Read).ShouldBeTrue();
    }

    [Fact]
    async Task should_repair_a_missed_broadcast_on_poll()
    {
        await _second.Refresh();
        _grain.GetVersion().Returns(Task.FromResult(2L));
        _grain.GetSnapshot().Returns(Task.FromResult(new StoreEntitlementsState(2, [])));
        await _second.Poll();
        _second.Snapshot.Version.ShouldEqual(2L);
    }

    [Fact]
    async Task should_renew_the_lease_when_poll_confirms_the_version()
    {
        await _first.Refresh();
        _time.Now += TimeSpan.FromMinutes(6);
        _first.IsFresh.ShouldBeFalse();
        await _first.Poll();
        _first.IsFresh.ShouldBeTrue();
    }

    [Fact]
    async Task should_retain_the_snapshot_but_expire_the_lease_after_failure()
    {
        await _first.Refresh();
        _time.Now += TimeSpan.FromMinutes(6);
        _grain.GetVersion().Returns<long>(_ => throw new IOException("unavailable"));
        await _first.Poll();
        _first.Snapshot.Version.ShouldEqual(1L);
        _first.IsFresh.ShouldBeFalse();
    }

    [Fact]
    void should_not_replace_a_newer_broadcast_with_an_older_one()
    {
        _first.Publish(new(3, []));
        _first.Publish(new(2, []));
        _first.Snapshot.Version.ShouldEqual(3L);
    }

    public class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
