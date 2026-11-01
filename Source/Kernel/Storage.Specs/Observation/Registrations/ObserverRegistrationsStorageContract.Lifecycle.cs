// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Observation.Registrations;

public abstract partial class ObserverRegistrationsStorageContract
{
    [Fact] public async Task should_query_active_registrations_and_owners_independently()
    {
        await Open().TrySave(Registration(), new(0));
        await Open().TrySave(Registration(sequence: "inactive") with { IsActive = false }, new(0));
        await Open().TrySave(Registration(sequence: "foreign") with { Owner = new("foreign") }, new(0));
        (await Open().GetActive()).Count().ShouldEqual(2);
        (await Open().GetByOwner(new("program"))).Count().ShouldEqual(2);
        (await Open().GetByOwner(new("unknown"))).ShouldBeEmpty();
    }
    [Fact] public async Task should_deactivate_and_reactivate_with_new_revisions()
    {
        var registration = Registration();
        await Open().TrySave(registration, new(0));
        (await Open().TrySave(registration with { IsActive = false, Revision = new(2) }, new(1))).ShouldBeTrue();
        (await Open().GetActive()).ShouldBeEmpty();
        (await Open().TrySave(registration with { Revision = new(3) }, new(2))).ShouldBeTrue();
        (await Open().GetActive()).Count().ShouldEqual(1);
    }
    [Fact] public async Task should_reject_stale_registration_writes()
    {
        var registration = Registration();
        await Open().TrySave(registration, new(0));
        await Open().TrySave(registration with { Revision = new(2) }, new(1));
        (await Open().TrySave(registration with { IsActive = false, Revision = new(2) }, new(1))).ShouldBeFalse();
        (await Open().Get(registration.ObserverId, registration.EventSequenceId))!.IsActive.ShouldBeTrue();
    }
    [Fact] public async Task should_only_allow_one_concurrent_registration_writer()
    {
        var registration = Registration();
        await Open().TrySave(registration, new(0));
        var writers = new[] { Open(), Open() };
        var results = await Task.WhenAll(writers.Select(storage => Task.Run(() => storage.TrySave(registration with { Revision = new(2) }, new(1)))));
        results.Count(result => result).ShouldEqual(1);
    }
    [Fact] public async Task should_only_allow_one_concurrent_registration_creator()
    {
        var writers = new[] { Open(), Open() };
        var results = await Task.WhenAll(writers.Select(storage => Task.Run(() => storage.TrySave(Registration(), new(0)))));
        results.Count(result => result).ShouldEqual(1);
    }
    [Fact] public async Task should_delete_only_the_expected_revision()
    {
        var registration = Registration();
        await Open().TrySave(registration, new(0));
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, new(2))).ShouldBeFalse();
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, new(1))).ShouldBeTrue();
        (await Open().Get(registration.ObserverId, registration.EventSequenceId)).ShouldBeNull();
    }
    [Fact] public async Task should_continue_revisions_after_delete_and_recreate()
    {
        var registration = Registration();
        (await Open().TrySave(registration, new(0))).ShouldBeTrue();
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, registration.Revision)).ShouldBeTrue();
        (await Open().TrySave(registration, new(0))).ShouldBeFalse();
        (await Open().TrySave(registration with { Revision = new(2) }, new(1))).ShouldBeFalse();
        (await Open().TrySave(registration with { Revision = new(3) }, new(2))).ShouldBeTrue();
        var recreated = (await Open().Get(registration.ObserverId, registration.EventSequenceId))!;
        recreated.Revision.Value.ShouldEqual(3UL);
        recreated.Revision.Value.ShouldBeGreaterThan(registration.Revision.Value);
        (await Open().GetActive()).Count().ShouldEqual(1);
        (await Open().GetByOwner(registration.Owner)).Count().ShouldEqual(1);
    }
    [Fact] public async Task should_reject_stale_writers_after_delete_and_recreate()
    {
        var registration = Registration();
        (await Open().TrySave(registration, new(0))).ShouldBeTrue();
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, registration.Revision)).ShouldBeTrue();
        var recreated = registration with { Owner = new("new-owner"), Revision = new(3) };
        (await Open().TrySave(recreated, new(2))).ShouldBeTrue();
        (await Open().TrySave(registration with { IsActive = false, Revision = new(2) }, registration.Revision)).ShouldBeFalse();
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, registration.Revision)).ShouldBeFalse();
        var stored = await Open().Get(registration.ObserverId, registration.EventSequenceId);
        JsonSerializer.Serialize(stored, SerializerOptions).ShouldEqual(JsonSerializer.Serialize(recreated, SerializerOptions));
    }
    [Fact] public async Task should_hide_tombstones_from_all_reads_and_queries()
    {
        var registration = Registration();
        (await Open().TrySave(registration, new(0))).ShouldBeTrue();
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, registration.Revision)).ShouldBeTrue();
        (await Open().Get(registration.ObserverId, registration.EventSequenceId)).ShouldBeNull();
        (await Open().GetActive()).ShouldBeEmpty();
        (await Open().GetByOwner(registration.Owner)).ShouldBeEmpty();
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, new(2))).ShouldBeFalse();
    }
    [Fact] public async Task should_reject_stale_deletes_without_changing_the_registration()
    {
        var registration = Registration();
        (await Open().TrySave(registration, new(0))).ShouldBeTrue();
        var updated = registration with { Revision = new(2) };
        (await Open().TrySave(updated, registration.Revision)).ShouldBeTrue();
        (await Open().TryDelete(registration.ObserverId, registration.EventSequenceId, registration.Revision)).ShouldBeFalse();
        var stored = await Open().Get(registration.ObserverId, registration.EventSequenceId);
        JsonSerializer.Serialize(stored, SerializerOptions).ShouldEqual(JsonSerializer.Serialize(updated, SerializerOptions));
    }
    [Fact] public async Task should_reject_non_monotonic_registration_revisions() => (await Open().TrySave(Registration() with { Revision = new(7) }, new(0))).ShouldBeFalse();
}
