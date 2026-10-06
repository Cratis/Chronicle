// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Objects;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Refreshes the owned plaintext snapshot with future property updates folded into the saved changeset.
/// </summary>
/// <param name="readModelsCompliance">The erasure fence for the refreshed snapshot.</param>
/// <param name="eventStore">The event store.</param>
/// <param name="eventStoreNamespace">The event store namespace.</param>
internal class RefreshReleasedReadModel(
    IReadModelsCompliance readModelsCompliance,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        if (context.IsDeferred || context.IsUnresolvable || context.FailedPartitions.Any() || context.ReleasedReadModel is null)
        {
            return context;
        }

        // SaveChanges folds these same-key property-only saves after EncryptChangeset captured its
        // plaintext snapshot. Apply their differences, never the protected main changeset or its state.
        var differences = context.PendingFutureSaves
            .Where(save => Equals(save.Key.Value, context.Key.Value) && save.Changeset.Changes.All(change => change is PropertiesChanged<ExpandoObject>))
            .SelectMany(save => save.Changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>())
            .SelectMany(change => change.Differences)
            .ToArray();
        if (differences.Length == 0)
        {
            return context;
        }

        foreach (var difference in differences)
        {
            difference.PropertyPath.SetValue(context.ReleasedReadModel, difference.Changed?.Clone()!, difference.ArrayIndexers);
        }

        // Future values are already plaintext. Check erasure without attempting to decrypt them again.
        context.ReleasedReadModel = await readModelsCompliance.ApplyErasureFence(
            eventStore,
            eventStoreNamespace,
            projection.TargetReadModelSchema,
            context.Event.Context.ResolveComplianceIdentifier(context.Key),
            context.ReleasedReadModel);
        return context;
    }
}
