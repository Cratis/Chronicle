// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that encrypts
/// PII fields in the current state and writes the compliance subject into the document before saving.
/// </summary>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for encrypting PII fields.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
/// <param name="objectComparer">The <see cref="IObjectComparer"/> for computing property differences.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> this step belongs to.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> this step belongs to.</param>
public class EncryptChangeset(
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter,
    IObjectComparer objectComparer,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        if (context.IsDeferred)
        {
            return context;
        }

        var identifier = context.Event.Context.Subject.Value;

        var schema = projection.TargetReadModelSchema;
        var currentState = context.Changeset.CurrentState;
        var currentStateAsDictionary = (IDictionary<string, object?>)currentState;
        currentStateAsDictionary.TryGetValue(WellKnownProperties.Subject, out var currentSubjectValue);
        var currentSubject = currentSubjectValue as string;

        var encrypted = await ReadModelComplianceHelper.Apply(
            complianceManager,
            eventStore,
            eventStoreNamespace,
            schema,
            identifier,
            currentState,
            expandoObjectConverter);

        var hasDifferences = !objectComparer.Compare(currentState, encrypted, out var differences);
        var propertyDifferences = hasDifferences && differences is not null
            ? differences.ToList()
            : [];

        var encryptedStateAsDictionary = (IDictionary<string, object?>)encrypted;
        if (encryptedStateAsDictionary.TryGetValue(WellKnownProperties.Subject, out var encryptedSubjectValue) &&
            encryptedSubjectValue is string encryptedSubject &&
            currentSubject != encryptedSubject &&
            propertyDifferences.TrueForAll(_ => _.PropertyPath != WellKnownProperties.Subject))
        {
            propertyDifferences.Add(new PropertyDifference(WellKnownProperties.Subject, currentSubject, encryptedSubject));
        }

        if (propertyDifferences.Count != 0)
        {
            context.Changeset.Add(new PropertiesChanged<System.Dynamic.ExpandoObject>(encrypted, propertyDifferences));
        }

        return context;
    }
}
