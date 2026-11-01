// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Registrations;

namespace Cratis.Chronicle.Storage.Observation.Registrations;

public abstract partial class ObserverRegistrationsStorageContract
{
    [Fact] public async Task should_not_create_a_floor_or_owner_settings_on_read()
    {
        (await Open().GetGenerationFloor()).ShouldBeNull();
        (await Open().GetOwnerSettings(new("program"))).ShouldBeNull();
    }
    [Fact] public async Task should_reject_stale_floor_versions_and_lower_generations()
    {
        (await Open().TrySaveGenerationFloor(new(10, 1), 0)).ShouldBeTrue();
        (await Open().TrySaveGenerationFloor(new(20, 2), 1)).ShouldBeTrue();
        (await Open().TrySaveGenerationFloor(new(30, 2), 1)).ShouldBeFalse();
        (await Open().TrySaveGenerationFloor(new(9, 3), 2)).ShouldBeFalse();
        (await Open().GetGenerationFloor()).ShouldEqual(new ObserverGenerationFloor(20, 2));
    }
    [Fact] public async Task should_preserve_unsigned_floor_generations_without_truncation()
    {
        (await Open().TrySaveGenerationFloor(new(ulong.MaxValue, 1), 0)).ShouldBeTrue();
        (await Open().TrySaveGenerationFloor(new(1, 2), 1)).ShouldBeFalse();
        (await Open().GetGenerationFloor())!.Generation.ShouldEqual(ulong.MaxValue);
    }
    [Fact] public async Task should_only_allow_one_concurrent_floor_writer()
    {
        await Open().TrySaveGenerationFloor(new(10, 1), 0);
        var writers = new[] { Open(), Open() };
        var results = await Task.WhenAll(writers.Select(storage => Task.Run(() => storage.TrySaveGenerationFloor(new(20, 2), 1))));
        results.Count(result => result).ShouldEqual(1);
    }
    [Fact] public async Task should_only_allow_one_concurrent_floor_creator()
    {
        var writers = new[] { Open(), Open() };
        var results = await Task.WhenAll(writers.Select(storage => Task.Run(() => storage.TrySaveGenerationFloor(new(10, 1), 0))));
        results.Count(result => result).ShouldEqual(1);
    }
    [Fact] public async Task should_persist_owner_timeout_and_principal_separately_from_observers()
    {
        var settings = new ObserverOwnerSettings(new("program"), new("subject")) { HttpDeliveryTimeout = TimeSpan.FromSeconds(45), Revision = 1 };
        (await Open().TrySaveOwnerSettings(settings, 0)).ShouldBeTrue();
        (await Open().GetOwnerSettings(settings.Owner)).ShouldEqual(settings);
        (await Open().TrySave(Registration(), new(0))).ShouldBeTrue();
        (await Open().TrySaveOwnerSettings(settings with { Revision = 2 }, 1)).ShouldBeTrue();
        (await Open().Get("observer", "event-log"))!.Revision.ShouldEqual(new ObserverRegistrationRevision(1));
    }
    [Fact] public async Task should_keep_owner_revisions_independent()
    {
        var settings = new ObserverOwnerSettings(new("program"), new("subject")) { Revision = 1 };
        await Open().TrySaveOwnerSettings(settings, 0);
        var foreign = settings with { Owner = new("foreign"), OwnerPrincipal = new("other"), HttpDeliveryTimeout = TimeSpan.FromSeconds(90) };
        (await Open().TrySaveOwnerSettings(foreign, 0)).ShouldBeTrue();
        await Open().TrySaveOwnerSettings(settings with { Revision = 2 }, 1);
        (await Open().TrySaveOwnerSettings(settings with { Revision = 2 }, 1)).ShouldBeFalse();
        (await Open().GetOwnerSettings(foreign.Owner)).ShouldEqual(foreign);
    }
    [Fact] public async Task should_only_allow_one_concurrent_owner_timeout_writer()
    {
        var settings = new ObserverOwnerSettings(new("program"), new("subject")) { Revision = 1 };
        await Open().TrySaveOwnerSettings(settings, 0);
        var writers = new[] { Open(), Open() };
        var results = await Task.WhenAll(writers.Select(storage => Task.Run(() => storage.TrySaveOwnerSettings(settings with { Revision = 2 }, 1))));
        results.Count(result => result).ShouldEqual(1);
    }
    [Fact] public async Task should_accept_timeouts_longer_than_the_default_without_an_arbitrary_limit()
    {
        var settings = new ObserverOwnerSettings(new("program"), new("subject")) { Revision = 1, HttpDeliveryTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1) };
        (await Open().TrySaveOwnerSettings(settings, 0)).ShouldBeTrue();
        (await Open().GetOwnerSettings(settings.Owner))!.HttpDeliveryTimeout.ShouldEqual(settings.HttpDeliveryTimeout);
    }
    [Fact] public async Task should_refuse_invalid_timeouts()
    {
        var settings = new ObserverOwnerSettings(new("program"), new("subject")) { Revision = 1 };
        (await Open().TrySaveOwnerSettings(settings with { HttpDeliveryTimeout = TimeSpan.Zero }, 0)).ShouldBeFalse();
        (await Open().TrySaveOwnerSettings(settings with { HttpDeliveryTimeout = TimeSpan.FromSeconds(-1) }, 0)).ShouldBeFalse();
        (await Open().TrySaveOwnerSettings(settings with { HttpDeliveryTimeout = TimeSpan.MaxValue }, 0)).ShouldBeFalse();
        (await Open().GetOwnerSettings(settings.Owner)).ShouldBeNull();
    }
}
