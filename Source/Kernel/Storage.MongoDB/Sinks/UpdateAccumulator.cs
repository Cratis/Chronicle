// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Accumulates the update operations, array filters and null parents a changeset is converted into.
/// </summary>
/// <remarks>
/// The first operation starts the <see cref="UpdateDefinition{TDocument}"/>; every later one is chained onto it, so
/// the order the operations are added in is the order MongoDB sees them.
/// </remarks>
internal sealed class UpdateAccumulator
{
    /// <summary>
    /// Gets the update built so far, or null when no operation has been added.
    /// </summary>
    public UpdateDefinition<BsonDocument>? Definition { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether anything has changed.
    /// </summary>
    public bool HasChanges { get; set; }

    /// <summary>
    /// Gets the array filters the operations are positioned with.
    /// </summary>
    public ArrayFilters ArrayFilters { get; } = new();

    /// <summary>
    /// Gets the paths of non-indexed parents that must be unset when null before a leaf is written beneath them.
    /// </summary>
    public HashSet<string> NullParentPaths { get; } = [];

    /// <summary>
    /// Gets the indexed parents that must be unset when null before a leaf is written beneath them.
    /// </summary>
    public List<NullArrayParent> NullArrayParents { get; } = [];

    /// <summary>
    /// Add a $set operation.
    /// </summary>
    /// <param name="property">The MongoDB property to set.</param>
    /// <param name="value">The value to set.</param>
    public void Set(string property, BsonValue value) =>
        Definition = Definition is not null ? Definition.Set(property, value) : Builders<BsonDocument>.Update.Set(property, value);

    /// <summary>
    /// Add a $max operation.
    /// </summary>
    /// <param name="property">The MongoDB property to move forward.</param>
    /// <param name="value">The value to take the maximum with.</param>
    public void Max(string property, BsonValue value) =>
        Definition = Definition is not null ? Definition.Max(property, value) : Builders<BsonDocument>.Update.Max(property, value);

    /// <summary>
    /// Add an $unset operation.
    /// </summary>
    /// <param name="property">The MongoDB property to unset.</param>
    public void Unset(string property) =>
        Definition = Definition is not null ? Definition.Unset(property) : Builders<BsonDocument>.Update.Unset(property);

    /// <summary>
    /// Add a plain $push operation.
    /// </summary>
    /// <param name="property">The MongoDB property of the array.</param>
    /// <param name="value">The value to push.</param>
    public void Push(string property, BsonValue value) =>
        Definition = Definition is not null ? Definition.Push(property, value) : Builders<BsonDocument>.Update.Push(property, value);

    /// <summary>
    /// Add a $push operation using $each with an optional $position and $sort.
    /// </summary>
    /// <param name="property">The MongoDB property of the array.</param>
    /// <param name="value">The value to push.</param>
    /// <param name="position">The optional position to push at.</param>
    /// <param name="sort">The optional sort to order the whole array by.</param>
    public void PushEach(string property, BsonValue value, int? position, SortDefinition<BsonValue>? sort) =>
        Definition = Definition is not null
            ? Definition.PushEach<BsonDocument, BsonValue>(property, [value], position: position, sort: sort)
            : Builders<BsonDocument>.Update.PushEach<BsonValue>(property, [value], position: position, sort: sort);

    /// <summary>
    /// Add a $pull operation by value.
    /// </summary>
    /// <param name="property">The MongoDB property of the array.</param>
    /// <param name="value">The value to pull.</param>
    public void Pull(string property, BsonValue value) =>
        Definition = Definition is not null ? Definition.Pull(property, value) : Builders<BsonDocument>.Update.Pull(property, value);

    /// <summary>
    /// Add a $pull operation by filter.
    /// </summary>
    /// <param name="property">The MongoDB property of the array.</param>
    /// <param name="filter">The filter selecting the elements to pull.</param>
    public void PullFilter(string property, FilterDefinition<BsonDocument> filter) =>
        Definition = Definition is not null ? Definition.PullFilter(property, filter) : Builders<BsonDocument>.Update.PullFilter(property, filter);

    /// <summary>
    /// Add the filters of a property, recording the array path the property is positioned in.
    /// </summary>
    /// <param name="property">The <see cref="MongoDBProperty"/> to add the filters of.</param>
    public void AddArrayFilters(MongoDBProperty property) => ArrayFilters.AddRange(property.Property, property.ArrayFilters);

    /// <summary>
    /// Record a missing parent that has to be unset when null before a leaf is written beneath it.
    /// </summary>
    /// <param name="parent">The <see cref="MongoDBProperty"/> of the parent.</param>
    public void AddNullParent(MongoDBProperty parent)
    {
        if (!parent.ArrayFilters.Any())
        {
            NullParentPaths.Add(parent.Property);
            return;
        }

        var filters = parent.ArrayFilters.ToArray();
        filters[^1] = RestrictToNullParent(parent.Property, filters[^1]);
        NullArrayParents.Add(new NullArrayParent(parent.Property, filters));
    }

    /// <summary>
    /// Gets the non-indexed null parent paths, shallowest first.
    /// </summary>
    /// <returns>The ordered paths.</returns>
    public string[] OrderedNullParentPaths() => NullParentPaths.OrderBy(DepthOf).ToArray();

    /// <summary>
    /// Gets the distinct indexed null parents.
    /// </summary>
    /// <returns>The distinct <see cref="NullArrayParent"/> instances.</returns>
    public NullArrayParent[] DistinctNullArrayParents() => NullArrayParents.DistinctBy(_ => (_.Path, string.Join('|', _.ArrayFilters.Select(filter => filter.Document.ToJson())))).ToArray();

    /// <summary>
    /// Gets the parents to repair before the main write, shallowest first.
    /// </summary>
    /// <returns>The <see cref="NullParentRepair"/> instances.</returns>
    public IEnumerable<NullParentRepair> NullParentRepairs() =>
        NullParentPaths.Select(path => new NullParentRepair(path, []))
            .Concat(DistinctNullArrayParents().Select(_ => new NullParentRepair(_.Path, _.ArrayFilters)))
            .OrderBy(_ => DepthOf(_.Path));

    static int DepthOf(string path) => path.Count(ch => ch == '.');

    static BsonDocumentArrayFilterDefinition<BsonDocument> RestrictToNullParent(string parentProperty, BsonDocumentArrayFilterDefinition<BsonDocument> innermostFilter)
    {
        var innermost = innermostFilter.Document.DeepClone().AsBsonDocument;
        var identifier = innermost.GetElement(0).Name.Split('.')[0];
        var marker = $".$[{identifier}].";
        var relativePath = parentProperty[(parentProperty.LastIndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
        innermost[$"{identifier}.{relativePath}"] = new BsonDocument("$type", "null");
        return new BsonDocumentArrayFilterDefinition<BsonDocument>(innermost);
    }
}
