// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySinkProvenanceStorage;

public class when_inventory_workers_finalize_in_a_different_order : Specification
{
    SinkProvenanceState _late;
    SinkProvenanceState _marked;
    SinkProvenanceState _markedBeforeCompletion;

    async Task Because()
    {
        var shared = new InMemorySinkProvenanceStorage();
        await shared.ReadState(); // Both workers saw incomplete inventory before either commits.
        await shared.ReadState();
        await shared.CompleteInventory(false);
        _late = await shared.CompleteInventory(true);
        await shared.MarkQuarantined();
        _marked = await shared.CompleteInventory(false);
        var another = new InMemorySinkProvenanceStorage();
        await another.MarkQuarantined();
        _markedBeforeCompletion = await another.CompleteInventory(false);
    }

    [Fact] void should_discard_late_inventory_evidence_after_completion() => _late.Restricted.ShouldBeFalse();
    [Fact] void should_publish_completion_atomically() => _late.IsComplete.ShouldBeTrue();
    [Fact] void should_never_discard_an_explicit_mark_after_completion() => _marked.Restricted.ShouldBeTrue();
    [Fact] void should_never_clear_a_mark_that_preceded_completion() => _markedBeforeCompletion.Restricted.ShouldBeTrue();
}
