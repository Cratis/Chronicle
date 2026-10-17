// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Verifies the same durable lifecycle-intent contract against each provider.
/// </summary>
public abstract class LifecycleFanOutContract : Specification
{
    LifecycleFanOutIntent _intent;
    LifecycleFanOutIntent? _durable;
    bool _first;
    bool _duplicate;
    IEnumerable<LifecycleFanOutIntent> _pending;

    /// <summary>
    /// Opens another storage instance over the same event-store database.
    /// </summary>
    /// <returns>The lifecycle intent store.</returns>
    protected abstract ILifecycleFanOutStorage OpenStorage();

    void Establish()
    {
        var id = OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, Guid.NewGuid().ToString());
        var occurrence = new Occurrence(id, new("startup"), OccurrenceKind.Startup, "fingerprint", "{}", 1, DateTimeOffset.UtcNow, null, null, OccurrenceState.Pending, 0, 0, null, null, null, null);
        _intent = new("activation", occurrence, ["first", "second"]);
    }

    async Task Because()
    {
        var storage = OpenStorage();
        _first = await storage.TryInsert(_intent);
        _duplicate = await OpenStorage().TryInsert(_intent with { Namespaces = ["later"] });
        await storage.Advance(_intent.Occurrence.Id, 1);
        await OpenStorage().Advance(_intent.Occurrence.Id, 0);
        _durable = await OpenStorage().Get(_intent.Occurrence.Id);
        await storage.Advance(_intent.Occurrence.Id, 2);
        _pending = (await OpenStorage().GetPending(100)).Where(_ => _.Occurrence.Id == _intent.Occurrence.Id).ToArray();
    }

    [Fact] void should_insert_the_first_intent() => _first.ShouldBeTrue();
    [Fact] void should_refuse_to_replace_the_snapshot() => _duplicate.ShouldBeFalse();
    [Fact] void should_retain_the_original_namespaces() => _durable!.Namespaces.ShouldContainOnly("first", "second");
    [Fact] void should_not_move_progress_backwards() => _durable!.Progress.ShouldEqual(1);
    [Fact] void should_not_list_completed_intents_as_pending() => _pending.ShouldBeEmpty();
}
