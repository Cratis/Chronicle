// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

internal sealed class ReleasedRevisionScenario(IEventSequence sequence, NewNodeStorage storage)
{
    const string Redacted = "EventRedacted";

    internal async Task Run()
    {
        await UnrevisedAppend();
        await ReleasedRevision();
        await InterleavedRevisions();
        await ReleasedRedaction(bySource: false);
        await ReleasedRedaction(bySource: true);
    }

    async Task UnrevisedAppend()
    {
        var original = Guid.NewGuid();
        var number = await Append(Guid.NewGuid().ToString(), original);
        (await storage.Read(number)).ShouldEqual(original);
        var row = await storage.Row(number);
        row.OriginalContent.ShouldBeNull();
        row.Revisions.ShouldBeNull();
        (await storage.Open().GetEventAt(new(number))).Revisions.ShouldBeEmpty();
        await AssertReleasedRead(number, original);
    }

    async Task ReleasedRevision()
    {
        var number = await Append(Guid.NewGuid().ToString(), Guid.NewGuid());
        await ReleasedReviseAndRead(number);
        await ReleasedReviseAndRead(number);
        var recorded = Guid.NewGuid();
        await storage.Revise(number, recorded);
        (await storage.Open().GetEventAt(new(number))).Revisions.Count().ShouldEqual(1);
        (await storage.Open().GetEventAt(new(number))).OriginalContent.ShouldBeEmpty();
        (await storage.Read(number)).ShouldEqual(recorded);
        await AssertReleasedRead(number, recorded);
    }

    async Task ReleasedReviseAndRead(ulong number)
    {
        var released = Guid.NewGuid();
        await sequence.Revise(new(number), new ClusterEventAppended(released));
        await WaitForReleasedRevision(number, released);
        (await storage.Read(number)).ShouldEqual(released);
        await AssertReleasedRead(number, released);
        var row = await storage.Row(number);
        row.OriginalContent.ShouldBeNull();
        row.Revisions.ShouldBeNull();
        var read = await storage.Open().GetEventAt(new(number));
        read.Revisions.ShouldBeEmpty();
        read.OriginalContent.ShouldBeEmpty();
    }

    async Task InterleavedRevisions()
    {
        var original = Guid.NewGuid();
        var number = await Append(Guid.NewGuid().ToString(), original);
        var recorded = Guid.NewGuid();
        await storage.Revise(number, recorded);
        await AssertReleasedRead(number, recorded);
        await AssertHistory(number, original, recorded, 1);
        await ReleasedOverwrite(number);
        await AssertHistory(number, original, recorded, 1);
        var resumed = Guid.NewGuid();
        await storage.Revise(number, resumed);
        await AssertHistory(number, original, resumed, 2);
        await AssertReleasedRead(number, resumed);
    }

    async Task ReleasedOverwrite(ulong number)
    {
        var before = await storage.Row(number);
        var unrecorded = Guid.NewGuid();
        await sequence.Revise(new(number), new ClusterEventAppended(unrecorded));
        await WaitForReleasedRevision(number, unrecorded);
        (await storage.Read(number)).ShouldEqual(unrecorded);
        await AssertReleasedRead(number, unrecorded);
        var after = await storage.Row(number);
        after.OriginalContent.ShouldEqual(before.OriginalContent);
        after.Revisions.ShouldEqual(before.Revisions);
    }

    async Task ReleasedRedaction(bool bySource)
    {
        var source = Guid.NewGuid().ToString();
        var number = await Append(source, Guid.NewGuid());
        await storage.Revise(number, Guid.NewGuid());
        (await storage.Row(number)).Revisions.ShouldNotBeNull();
        if (bySource) await sequence.Redact(new EventSourceId(source), "rolling-upgrade-test");
        else await sequence.Redact(new EventSequenceNumber(number), "rolling-upgrade-test");
        await storage.WaitForRow(number, row => row.Type.Value == Redacted, "released redaction");
        var redacted = await storage.Open().GetEventAt(new(number));
        redacted.Revisions.ShouldBeEmpty();
        redacted.OriginalContent.ShouldBeEmpty();

        // A released EF model cannot clear columns it does not know about.
        (await storage.Row(number)).Revisions.ShouldNotBeNull();
        await storage.EnsureTableExists();
        var scrubbed = await storage.Row(number);
        scrubbed.OriginalContent.ShouldBeNull();
        scrubbed.Revisions.ShouldBeNull();
        var reread = await storage.Open().GetEventAt(new(number));
        reread.Revisions.ShouldBeEmpty();
        JsonSerializer.Serialize(reread.Content).ShouldEqual(JsonSerializer.Serialize(redacted.Content));
    }

    Task WaitForReleasedRevision(ulong number, Guid expected) =>
        storage.WaitForRow(number, row => row.Content.Contains(expected.ToString(), StringComparison.OrdinalIgnoreCase), "released revision");

    async Task<ulong> Append(string source, Guid id)
    {
        var result = await sequence.Append(source, new ClusterEventAppended(id));
        result.IsSuccess.ShouldBeTrue();
        await storage.WaitForRow(result.SequenceNumber.Value, row => row.Content.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase), "released append");
        return result.SequenceNumber.Value;
    }

    async Task AssertReleasedRead(ulong number, Guid expected)
    {
        var events = await sequence.GetFromSequenceNumber(new(number));
        var read = events.Single(@event => @event.Context.SequenceNumber.Value == number);
        ((ClusterEventAppended)read.Content).Id.ShouldEqual(expected);
    }

    async Task AssertHistory(ulong number, Guid original, Guid last, int count)
    {
        var read = await storage.Open().GetEventAt(new(number));
        read.Revisions.Count().ShouldEqual(count);
        using var originalContent = JsonDocument.Parse(read.OriginalContent);
        originalContent.RootElement.GetProperty(nameof(ClusterEventAppended.Id)).GetGuid().ShouldEqual(original);
        using var revision = JsonDocument.Parse(read.Revisions.Last().Content);
        revision.RootElement.GetProperty(nameof(ClusterEventAppended.Id)).GetGuid().ShouldEqual(last);
        read.GenerationalHashes.ShouldNotBeEmpty();
    }
}
