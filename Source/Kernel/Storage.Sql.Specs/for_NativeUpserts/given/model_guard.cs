// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Chronicle.Storage.Sql.for_NativeUpserts.given;

public static class model_guard
{
    public static IEnumerable<string> Unsupported(Action<DbContextOptionsBuilder> configure)
    {
        var unsupported = new List<string>();
        using var cluster = new ClusterDbContext(Options<ClusterDbContext>(configure));
        using var store = new EventStoreDbContext(Options<EventStoreDbContext>(configure));
        using var ns = new NamespaceDbContext(Options<NamespaceDbContext>(configure));
        Check(cluster, [nameof(ClusterDbContext.Reminders), nameof(ClusterDbContext.EventStores), nameof(ClusterDbContext.Users), nameof(ClusterDbContext.Applications), nameof(ClusterDbContext.DataProtectionKeys), nameof(ClusterDbContext.Patches), nameof(ClusterDbContext.SystemInformation), nameof(ClusterDbContext.Tokens), nameof(ClusterDbContext.Authorizations), nameof(ClusterDbContext.Scopes)], unsupported);
        Check(store, [nameof(EventStoreDbContext.EventStoreInitialization), nameof(EventStoreDbContext.EventTypes), nameof(EventStoreDbContext.Observers), nameof(EventStoreDbContext.Projections), nameof(EventStoreDbContext.Reactors), nameof(EventStoreDbContext.Reducers), nameof(EventStoreDbContext.ReadModels), nameof(EventStoreDbContext.WebhookDefinitions), nameof(EventStoreDbContext.EventSourceDefinitions), nameof(EventStoreDbContext.ExternalServiceDefinitions), nameof(EventStoreDbContext.SequenceQueries), nameof(EventStoreDbContext.SequenceQueryFolders), nameof(EventStoreDbContext.Captures), nameof(EventStoreDbContext.CaptureObservations), nameof(EventStoreDbContext.EventStoreSubscriptions), nameof(EventStoreDbContext.EventSeeds)], unsupported);
        Check(ns, [nameof(NamespaceDbContext.FailedPartitions), nameof(NamespaceDbContext.Recommendations), nameof(NamespaceDbContext.BehaviorPatterns), nameof(NamespaceDbContext.ReplayContexts), nameof(NamespaceDbContext.ReplayedModels), nameof(NamespaceDbContext.EventSequences), nameof(NamespaceDbContext.ProjectionFutures), nameof(NamespaceDbContext.EncryptionKeys), nameof(NamespaceDbContext.EncryptionKeyErasures), nameof(NamespaceDbContext.EventSeeds)], unsupported);
        return unsupported;
    }

    static DbContextOptions<T> Options<T>(Action<DbContextOptionsBuilder> configure)
        where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        configure(builder);
        return builder.AddConceptAsSupport().WithoutServiceProviderCaching().Options;
    }

    static void Check(DbContext context, string[] sets, List<string> unsupported)
    {
        foreach (var set in sets)
        {
            var type = context.GetType().GetProperty(set)!.PropertyType.GetGenericArguments().Single();
            var entity = context.Model.FindEntityType(type)!;
            if (entity.FindPrimaryKey() is null || entity.BaseType is not null || entity.GetDerivedTypes().Any() ||
                entity.GetTableMappings().Count() != 1 || entity.GetComplexProperties().Any() || entity.IsOwned() ||
                entity.GetNavigations().Any() || entity.GetSkipNavigations().Any() ||
                entity.GetProperties().Any(property => property.IsShadowProperty() ||
                    (property.IsPrimaryKey() && property.ValueGenerated != ValueGenerated.Never &&
                        property.ClrType != typeof(Guid) && property.ClrType != typeof(string)) ||
                    (context.Database.IsSqlServer() && SqlServerPropertyExtensions.GetValueGenerationStrategy(property) == SqlServerValueGenerationStrategy.IdentityColumn)))
            {
                unsupported.Add($"{context.GetType().Name}.{set}");
            }
        }
    }
}
