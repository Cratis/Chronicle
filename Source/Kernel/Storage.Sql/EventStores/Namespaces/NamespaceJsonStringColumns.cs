// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Pairs (entity type, property name) for columns whose value is a pre-serialized JSON string
/// stored in a provider-native JSON column. Used by <see cref="NamespaceDbContext.OnModelCreating"/>
/// to ensure EF Core sends the parameter with the matching provider type — only PostgreSQL
/// requires the explicit binding because its <c>jsonb</c> type does not accept implicit casts
/// from <c>text</c>. This is a temporary workaround until <c>JsonColumn&lt;string&gt;</c> in
/// <c>Cratis.Arc.EntityFrameworkCore</c> registers the entity column mapping automatically.
/// </summary>
internal static class NamespaceJsonStringColumns
{
    /// <summary>
    /// The (entity type, property name) pairs to bind to the provider-native JSON column type.
    /// </summary>
    public static readonly (System.Type EntityType, string PropertyName)[] All =
    [
        (typeof(Jobs.Job), "StateJson"),
        (typeof(JobSteps.JobStep), "StateJson"),
        (typeof(FailedPartitions.FailedPartition), "StateJson"),
        (typeof(Recommendations.Recommendation), "RequestJson"),
        (typeof(Projections.ProjectionFutureEntity), "EventContentJson"),
        (typeof(Projections.ProjectionFutureEntity), "ParentKeyJson"),
        (typeof(Changesets.Changeset), "ChangesetData"),
        (typeof(Seeding.EventSeedsEntity), "ByEventTypeJson"),
        (typeof(Seeding.EventSeedsEntity), "ByEventSourceJson"),
    ];
}
