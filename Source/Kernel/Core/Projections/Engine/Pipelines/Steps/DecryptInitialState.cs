// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that decrypts
/// PII fields in the initial state using the stored <see cref="WellKnownProperties.Subject"/> field.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for decrypting PII fields.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> this step belongs to.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> this step belongs to.</param>
public class DecryptInitialState(
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        if (context.Changeset.InitialState is null)
        {
            return context;
        }

        var schema = projection.TargetReadModelSchema;
        if (!schema.HasComplianceMetadata())
        {
            return context;
        }

        if (!((IDictionary<string, object?>)context.Changeset.InitialState).ContainsKey(WellKnownProperties.Subject))
        {
            return context;
        }

        context.Changeset.InitialState = await ReadModelComplianceHelper.Release(
            complianceManager,
            eventStore,
            eventStoreNamespace,
            schema,
            context.Changeset.InitialState,
            expandoObjectConverter);
        return context;
    }
}
