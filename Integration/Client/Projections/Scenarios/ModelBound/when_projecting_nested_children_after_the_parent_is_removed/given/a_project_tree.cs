// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_nested_children_after_the_parent_is_removed.given;

/// <summary>
/// Appends a project tree's history, lets the projection observe it and then replays it, so the history is
/// processed both live and the way a replay or catch-up processes it, in bulk. The read model has the shape of Studio's
/// ProjectHierarchy, trimmed: project, applications, domains and event models, each removal routed through a parent key.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleFixture"/>.</param>
public class a_project_tree(ChronicleFixture fixture) : Specification(fixture)
{
    public Guid ProjectId = Guid.NewGuid();
    public Guid ApplicationId = Guid.NewGuid();
    public Guid SiblingApplicationId = Guid.NewGuid();
    public Guid DomainId = Guid.NewGuid();
    public Guid SiblingDomainId = Guid.NewGuid();
    public Guid EventModelId = Guid.NewGuid();
    public IEnumerable<FailedPartition> FailedPartitionsAfterObserving = [];
    public IEnumerable<FailedPartition> FailedPartitions = [];
    public IEnumerable<BsonDocument> DocumentsAfterObserving = [];
    public IEnumerable<BsonDocument> Documents = [];

    EventSequenceNumber _lastAppended = EventSequenceNumber.First;
    IProjectionHandler _handler = null!;

    public override IEnumerable<Type> EventTypes =>
    [
        typeof(TreeProjectRegistered),
        typeof(TreeProjectRemoved),
        typeof(TreeApplicationCreatedForProject),
        typeof(TreeApplicationRemovedFromProject),
        typeof(TreeDomainCreatedForApplication),
        typeof(TreeDomainRenamed),
        typeof(TreeDomainRemoved),
        typeof(TreeEventModelCreatedForDomain)
    ];

    public override IEnumerable<Type> ModelBoundProjections => [typeof(TreeProject)];

    /// <summary>
    /// Gets whether the run stores read models in MongoDB, where the documents can be inspected directly.
    /// </summary>
    public bool CanInspectDocuments => ChronicleFixture.Options.StorageProvider == ChronicleStorageProvider.MongoDB;

    public BsonDocument? ProjectDocument => Documents.SingleOrDefault(_ => GuidOf(_["_id"]) == ProjectId);

    /// <summary>
    /// Gets the ids of the applications stored in the project document, matching field names whatever the naming policy.
    /// </summary>
    public IEnumerable<Guid?> StoredApplicationIds =>
        ProjectDocument?.Elements.FirstOrDefault(_ => _.Name.Equals(nameof(TreeProject.Applications), StringComparison.OrdinalIgnoreCase)).Value is BsonArray applications ?
            applications.Select(_ => GuidOf(_.AsBsonDocument["_id"])) :
            [];

    /// <summary>
    /// Gets whether every stored document is at most a placeholder: not initialized and holding none of the project's data.
    /// A child event for a root that is gone stores such a placeholder, by design, the way it does for a child arriving
    /// before its root; it must never bring the removed project, or its removed applications, back.
    /// </summary>
    /// <param name="documents">The stored documents.</param>
    /// <returns>True if only placeholders remain.</returns>
    public static bool OnlyPlaceholdersRemain(IEnumerable<BsonDocument> documents) =>
        documents.All(document =>
            document.TryGetValue("__initialized", out var initialized) && initialized == BsonBoolean.False &&
            !document.Names.Any(name =>
                name.Equals(nameof(TreeProject.Name), StringComparison.OrdinalIgnoreCase) ||
                name.Equals(nameof(TreeProject.Applications), StringComparison.OrdinalIgnoreCase)));

    static Guid? GuidOf(BsonValue value) => value switch
    {
        BsonBinaryData binary when binary.SubType is BsonBinarySubType.UuidStandard => binary.ToGuid(),
        BsonBinaryData binary when binary.SubType is BsonBinarySubType.UuidLegacy => binary.ToGuid(GuidRepresentation.CSharpLegacy),
        BsonString text when Guid.TryParse(text.Value, out var guid) => guid,
        _ => null
    };

    protected Task Append(Guid eventSourceId, object @event) => Append(eventSourceId, [@event]);

    protected async Task Append(Guid eventSourceId, IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            var result = await EventStore.EventLog.Append(eventSourceId, @event);
            _lastAppended = result.SequenceNumber;
        }
    }

    protected async Task Subscribe()
    {
        var projectionId = EventStore.Projections.GetProjectionIdForModel<TreeProject>();
        _handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
        await _handler.WaitTillSubscribed();
    }

    protected async Task ObserveThenReplay()
    {
        await _handler.WaitTillReachesEventSequenceNumber(_lastAppended);
        FailedPartitionsAfterObserving = await _handler.GetFailedPartitions();
        DocumentsAfterObserving = await ReadDocuments();

        var replayJobId = await EventStore.Projections.Replay(_handler.Id);
        await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replayJobId);
        await _handler.WaitTillReachesEventSequenceNumber(_lastAppended);

        FailedPartitions = await _handler.GetFailedPartitions();
        Documents = await ReadDocuments();
    }

    async Task<IEnumerable<BsonDocument>> ReadDocuments()
    {
        if (!CanInspectDocuments)
        {
            return [];
        }

        var collectionName = Services.GetRequiredService<INamingPolicy>().GetReadModelName(typeof(TreeProject));
        return await ReadModelsDatabase.Database
            .GetCollection<BsonDocument>(collectionName)
            .Find(FilterDefinition<BsonDocument>.Empty)
            .ToListAsync();
    }
}
