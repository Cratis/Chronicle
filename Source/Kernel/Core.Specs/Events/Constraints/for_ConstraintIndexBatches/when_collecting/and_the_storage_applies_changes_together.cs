// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexBatches.when_collecting;

public class and_the_storage_applies_changes_together : Specification
{
    const string ClaimingEvent = "Claimed";
    const string ReleasingEvent = "Released";
    const string PropertyName = "name";

    UniqueConstraintValidator _validator;
    ConstraintIndexBatches _batches;
    bool _claimTaken;
    bool _releaseTaken;
    bool _unrelatedTaken;
    IReadOnlyList<UniqueConstraintIndexChange> _changes;

    void Establish()
    {
        var definition = new UniqueConstraintDefinition("names", [new(ClaimingEvent, [PropertyName])], RemovedWith: [ReleasingEvent]);
        _validator = new UniqueConstraintValidator(definition, Substitute.For<IUniqueConstraintsBatchStorage>());
        _batches = new();
    }

    void Because()
    {
        _claimTaken = _batches.TryAdd(_validator, UpdaterFor("first", ClaimingEvent, "a name"), 1);
        _releaseTaken = _batches.TryAdd(_validator, UpdaterFor("second", ReleasingEvent, null), 2);
        _unrelatedTaken = _batches.TryAdd(_validator, UpdaterFor("third", "Unrelated", "ignored"), 3);
        _changes = _batches.Batches.Single().Changes;
    }

    [Fact] void should_take_the_claim() => _claimTaken.ShouldBeTrue();
    [Fact] void should_take_the_release() => _releaseTaken.ShouldBeTrue();
    [Fact] void should_take_the_event_that_changes_nothing() => _unrelatedTaken.ShouldBeTrue();
    [Fact] void should_collect_only_the_changes_in_append_order() => _changes.Select(_ => _.SequenceNumber.Value).ShouldContainOnly(1UL, 2UL);
    [Fact] void should_describe_the_claim_as_a_save() => _changes[0].Kind.ShouldEqual(UniqueConstraintIndexChangeKind.Save);
    [Fact] void should_describe_the_release_as_a_remove() => _changes[1].Kind.ShouldEqual(UniqueConstraintIndexChangeKind.Remove);
    [Fact] void should_keep_the_claiming_event_source() => _changes[0].EventSourceId.ShouldEqual((EventSourceId)"first");

    IUpdateConstraintIndex UpdaterFor(EventSourceId eventSourceId, EventTypeId eventTypeId, string? name)
    {
        var content = new ExpandoObject();
        if (name is not null) ((IDictionary<string, object?>)content)[PropertyName] = name;
        var context = new ConstraintValidationContext([_validator], eventSourceId, eventTypeId, content);
        return context.IndexUpdates.Single().Updater;
    }
}
