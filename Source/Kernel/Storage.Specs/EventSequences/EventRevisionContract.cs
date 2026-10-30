// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Provider-independent behaviors of revising an event: the original content and every revision are kept, the
/// latest revision is what a read delivers, and a redaction clears the history. Each fact opens its own storage.
/// </summary>
public abstract class EventRevisionContract : Specification
{
    /// <summary>The event type, generation 1.</summary>
    protected static readonly EventType FirstGeneration = new("e4b1c3a0-5b0e-4f4e-9a55-7a1d3a6e9c11", EventTypeGeneration.First);

    /// <summary>The event type, generation 2.</summary>
    protected static readonly EventType SecondGeneration = new(FirstGeneration.Id, new EventTypeGeneration(2));

    static readonly IdentityId _appenderId = Guid.Parse("00000000-0000-0000-0000-0000000000a0");
    static readonly IdentityId _aliceId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    static readonly IdentityId _bobId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");
    static readonly Identity _appender = new(_appenderId.Value.ToString(), "appender", "appender");
    static readonly Identity _alice = new(_aliceId.Value.ToString(), "alice", "alice");
    static readonly Identity _bob = new(_bobId.Value.ToString(), "bob", "bob");

    /// <summary>
    /// Gets the identity storage the opened storage must resolve caused-by chains through.
    /// </summary>
    protected IIdentityStorage Identities { get; } = CreateIdentities();

    /// <summary>
    /// Gets the event types storage the opened storage must resolve schemas through.
    /// </summary>
    protected IEventTypesStorage EventTypes { get; } = CreateEventTypes();

    /// <summary>
    /// Opens a handle to the sequence of this fact. Calling it again returns another handle to the same sequence, which may be the same instance.
    /// </summary>
    /// <returns>The storage.</returns>
    protected abstract Task<IEventSequenceStorage> OpenStorage();

    [Fact]
    public async Task should_read_an_unrevised_event_without_revision_history()
    {
        var storage = await OpenStorage();
        await AppendOriginal(storage, 0);

        var read = await storage.GetEventAt(0);

        read.IsRevised.ShouldBeFalse();
        read.Revisions.Count().ShouldEqual(0);
        read.OriginalContent.ShouldEqual(string.Empty);
        Json(read.Content).ShouldEqual("""{"value":"original"}""");
        read.Context.Hash.ShouldEqual(new EventHash("hash-original"));
    }

    [Fact]
    public async Task should_keep_original_content_after_one_revision()
    {
        var storage = await OpenStorage();
        await AppendOriginal(storage, 0);
        var revision = new RevisionToMake(_alice, "first revision", "hash-first");
        await Revise(storage, 0, FirstGeneration, revision);

        var read = await storage.GetEventAt(0);

        SameJson(read.OriginalContent, "original");
        read.Revisions.Count().ShouldEqual(1);
        AssertRevision(read.Revisions.Single(), FirstGeneration.Generation, revision);
        Json(read.Content).ShouldEqual("""{"value":"first revision"}""");
        read.Context.Hash.ShouldEqual(new EventHash("hash-first"));
        read.IsRevised.ShouldBeTrue();
        (read.RevisedGeneration ?? read.Revisions.Last().EventTypeGeneration).ShouldEqual(FirstGeneration.Generation);
        SameJson(read.GenerationalContent[1], "original");
        read.GenerationalHashes[1].ShouldEqual(new EventHash("hash-original"));
    }

    [Fact]
    public async Task should_keep_every_revision_in_order_after_two_revisions()
    {
        var storage = await OpenStorage();
        await AppendOriginal(storage, 0);
        var first = new RevisionToMake(_alice, "first revision", "hash-first");
        var second = new RevisionToMake(_bob, "second revision", "hash-second");
        await Revise(storage, 0, FirstGeneration, first);
        await Revise(storage, 0, FirstGeneration, second);

        var read = await storage.GetEventAt(0);

        var revisions = read.Revisions.ToArray();
        revisions.Length.ShouldEqual(2);
        AssertRevision(revisions[0], FirstGeneration.Generation, first);
        AssertRevision(revisions[1], FirstGeneration.Generation, second);
        SameJson(read.OriginalContent, "original");
        Json(read.Content).ShouldEqual("""{"value":"second revision"}""");
        read.Context.Hash.ShouldEqual(new EventHash("hash-second"));
    }

    [Fact]
    public async Task should_revise_across_generations()
    {
        var storage = await OpenStorage();
        await AppendBothGenerations(storage, 0);
        var revision = new RevisionToMake(_alice, "second generation revision", "hash-revised-2");
        await Revise(storage, 0, SecondGeneration, revision);

        var read = await storage.GetEventAt(0);

        read.Context.EventType.Generation.ShouldEqual(SecondGeneration.Generation);
        Json(read.Content).ShouldEqual("""{"value":"second generation revision"}""");
        read.Context.Hash.ShouldEqual(new EventHash("hash-revised-2"));
        SameJson(read.OriginalContent, "original");
        read.Revisions.Count().ShouldEqual(1);
        AssertRevision(read.Revisions.Single(), SecondGeneration.Generation, revision);
        SameJson(read.GenerationalContent[1], "original");
        SameJson(read.GenerationalContent[2], "migrated");
        read.GenerationalHashes[1].ShouldEqual(new EventHash("hash-original"));
        read.GenerationalHashes[2].ShouldEqual(new EventHash("hash-migrated"));
    }

    [Fact]
    public async Task should_have_no_original_content_when_generation_one_was_never_stored()
    {
        var storage = await OpenStorage();
        await Append(storage, 0, SecondGeneration, new Dictionary<EventTypeGeneration, ExpandoObject> { [SecondGeneration.Generation] = Content("migrated") }, new Dictionary<EventTypeGeneration, EventHash> { [SecondGeneration.Generation] = "hash-migrated" });
        var revision = new RevisionToMake(_alice, "second generation revision", "hash-revised-2");
        await Revise(storage, 0, SecondGeneration, revision);

        var read = await storage.GetEventAt(0);

        read.OriginalContent.ShouldEqual(string.Empty);
        read.Revisions.Count().ShouldEqual(1);
        Json(read.Content).ShouldEqual("""{"value":"second generation revision"}""");
    }

    [Fact]
    public async Task should_clear_revisions_when_redacting_a_revised_event()
    {
        var storage = await OpenStorage();
        await AppendOriginal(storage, 0);
        await Revise(storage, 0, FirstGeneration, new RevisionToMake(_alice, "first revision", "hash-first"));

        await storage.Redact(0, "reason", CorrelationId.New(), [], [_aliceId], DateTimeOffset.UtcNow);
        var read = await storage.GetEventAt(0);

        read.Revisions.Count().ShouldEqual(0);
        read.IsRevised.ShouldBeFalse();
        read.OriginalContent.ShouldEqual(string.Empty);
        read.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    }

    [Fact]
    public async Task should_clear_revisions_when_redacting_by_event_source()
    {
        var storage = await OpenStorage();
        await AppendOriginal(storage, 0);
        await Revise(storage, 0, FirstGeneration, new RevisionToMake(_alice, "first revision", "hash-first"));

        await storage.Redact(EventSource, "reason", null, CorrelationId.New(), [], [_aliceId], DateTimeOffset.UtcNow);
        var read = await storage.GetEventAt(0);

        read.Revisions.Count().ShouldEqual(0);
        read.IsRevised.ShouldBeFalse();
        read.OriginalContent.ShouldEqual(string.Empty);
        read.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    }

    [Fact]
    public async Task should_keep_both_concurrent_revisions()
    {
        var first = await OpenStorage();
        var second = await OpenStorage();
        await AppendOriginal(first, 0);
        var byAlice = new RevisionToMake(_alice, "first revision", "hash-first");
        var byBob = new RevisionToMake(_bob, "second revision", "hash-second");

        await Task.WhenAll(Revise(first, 0, FirstGeneration, byAlice), Revise(second, 0, FirstGeneration, byBob));
        var read = await first.GetEventAt(0);

        var revisions = read.Revisions.ToArray();
        revisions.Length.ShouldEqual(2);
        string.Join(',', revisions.Select(_ => _.CausedBy.Name).Order()).ShouldEqual("alice,bob");
        SameJson(read.OriginalContent, "original");
        var last = revisions[^1];
        Json(read.Content).ShouldEqual(Json(JsonNode.Parse(last.Content)!));
        read.Context.Hash.ShouldEqual(new EventHash(last.CausedBy.Name == "alice" ? "hash-first" : "hash-second"));
    }

    /// <summary>
    /// Gets the event source every event in this contract is appended for.
    /// </summary>
    protected static EventSourceId EventSource { get; } = "revision-contract-source";

    /// <summary>
    /// Describes a revision to make.
    /// </summary>
    /// <param name="CausedBy">The identity that made the revision.</param>
    /// <param name="Value">The revised value.</param>
    /// <param name="Hash">The revised hash.</param>
    protected record RevisionToMake(Identity CausedBy, string Value, string Hash)
    {
        /// <summary>
        /// Gets the correlation of the revision.
        /// </summary>
        public CorrelationId CorrelationId { get; } = CorrelationId.New();

        /// <summary>
        /// Gets the causation of the revision.
        /// </summary>
        public Causation Causation { get; } = new(DateTimeOffset.UtcNow, CausationType.Unknown, new Dictionary<string, string> { ["reason"] = Value });

        /// <summary>
        /// Gets when the revision occurred, at whole-second precision.
        /// </summary>
        public DateTimeOffset Occurred { get; } = new(DateTimeOffset.UtcNow.UtcDateTime.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, TimeSpan.Zero);
    }

    static IIdentityStorage CreateIdentities()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(call =>
        {
            var id = call.Arg<IEnumerable<IdentityId>>().FirstOrDefault();
            if (id == _aliceId) return Task.FromResult(_alice);
            if (id == _bobId) return Task.FromResult(_bob);
            if (id == _appenderId) return Task.FromResult(_appender);
            return Task.FromResult(Identity.System);
        });
        return identities;
    }

    static IEventTypesStorage CreateEventTypes()
    {
        var eventTypes = Substitute.For<IEventTypesStorage>();
        var schema = Cratis.Chronicle.Schemas.JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string"}}}""").GetAwaiter().GetResult();
        eventTypes.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(true);
        eventTypes.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(call => new EventTypeSchema(new EventType(call.Arg<EventTypeId>(), call.Arg<EventTypeGeneration?>() ?? EventTypeGeneration.First), EventTypeOwner.None, EventTypeSource.Unknown, schema));
        return eventTypes;
    }

    static ExpandoObject Content(string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = value;
        return content;
    }

    static string Json(ExpandoObject content) => Json(JsonSerializer.SerializeToNode(content)!);

    static string Json(JsonNode node) => node.ToJsonString();

    static void SameJson(string actual, string expectedValue)
    {
        actual.ShouldNotEqual(string.Empty);
        JsonNode.DeepEquals(JsonNode.Parse(actual), JsonNode.Parse(Json(Content(expectedValue)))).ShouldBeTrue();
    }

    static Task AppendOriginal(IEventSequenceStorage storage, ulong sequenceNumber) =>
        Append(storage, sequenceNumber, FirstGeneration, new Dictionary<EventTypeGeneration, ExpandoObject> { [FirstGeneration.Generation] = Content("original") }, new Dictionary<EventTypeGeneration, EventHash> { [FirstGeneration.Generation] = "hash-original" });

    static Task AppendBothGenerations(IEventSequenceStorage storage, ulong sequenceNumber) =>
        Append(
            storage,
            sequenceNumber,
            SecondGeneration,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [FirstGeneration.Generation] = Content("original"), [SecondGeneration.Generation] = Content("migrated") },
            new Dictionary<EventTypeGeneration, EventHash> { [FirstGeneration.Generation] = "hash-original", [SecondGeneration.Generation] = "hash-migrated" });

    static async Task Append(IEventSequenceStorage storage, ulong sequenceNumber, EventType eventType, IDictionary<EventTypeGeneration, ExpandoObject> content, IDictionary<EventTypeGeneration, EventHash> hashes)
    {
        var result = await storage.Append(
            sequenceNumber,
            EventSourceType.Default,
            EventSource,
            EventStreamType.All,
            EventStreamId.Default,
            eventType,
            CorrelationId.New(),
            [],
            [_appenderId],
            [],
            DateTimeOffset.UtcNow,
            content,
            hashes);
        result.IsSuccess.ShouldBeTrue();
    }

    static Task Revise(IEventSequenceStorage storage, ulong sequenceNumber, EventType eventType, RevisionToMake revision) =>
        storage.Revise(
            sequenceNumber,
            eventType,
            revision.CorrelationId,
            [revision.Causation],
            [revision.CausedBy == _alice ? _aliceId : _bobId],
            revision.Occurred,
            Content(revision.Value),
            revision.Hash);

    static void AssertRevision(EventRevision actual, EventTypeGeneration generation, RevisionToMake expected)
    {
        actual.EventTypeGeneration.ShouldEqual(generation);
        actual.CorrelationId.ShouldEqual(expected.CorrelationId);
        actual.CausedBy.Name.ShouldEqual(expected.CausedBy.Name);
        actual.Occurred.UtcDateTime.ShouldEqual(expected.Occurred.UtcDateTime);
        actual.Causation.Count().ShouldEqual(1);
        actual.Causation.Single().Properties["reason"].ShouldEqual(expected.Value);
        SameJson(actual.Content, expected.Value);
    }
}
