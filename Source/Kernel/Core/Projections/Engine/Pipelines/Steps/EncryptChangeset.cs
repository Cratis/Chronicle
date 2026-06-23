// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that encrypts
/// PII fields in the current state and writes the compliance subject into the document before saving.
/// </summary>
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for encrypting PII fields.</param>
/// <param name="objectComparer">The <see cref="IObjectComparer"/> for computing property differences.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> this step belongs to.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> this step belongs to.</param>
public class EncryptChangeset(
    IReadModelsCompliance readModelsCompliance,
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

        var identifier = context.Event.Context.Subject?.IsSet == true
            ? context.Event.Context.Subject.Value
            : context.Event.Context.EventSourceId.Value;

        var schema = projection.TargetReadModelSchema;
        var currentState = context.Changeset.CurrentState;
        var currentStateAsDictionary = (IDictionary<string, object?>)currentState;
        currentStateAsDictionary.TryGetValue(WellKnownProperties.Subject, out var currentSubjectValue);
        var currentSubject = currentSubjectValue as string;

        var encrypted = await readModelsCompliance.Apply(
            eventStore,
            eventStoreNamespace,
            schema,
            identifier,
            currentState);

        var hasDifferences = !objectComparer.Compare(currentState, encrypted, out var differences);
        var propertyDifferences = hasDifferences && differences is not null
            ? differences.ToList()
            : [];

        var encryptedStateAsDictionary = (IDictionary<string, object?>)encrypted;
        if (encryptedStateAsDictionary.TryGetValue(WellKnownProperties.Subject, out var encryptedSubjectValue) &&
            encryptedSubjectValue is string encryptedSubject &&
            currentSubject is null &&
            context.Changeset.Changes.All(_ => _ is not ChildRemovedFromAll) &&
            propertyDifferences.TrueForAll(_ => _.PropertyPath != WellKnownProperties.Subject))
        {
            propertyDifferences.Add(new PropertyDifference(WellKnownProperties.Subject, currentSubject, encryptedSubject));
        }

        if (propertyDifferences.Count != 0)
        {
            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(encrypted, propertyDifferences));
        }

        // Child-collection changes (ChildAdded, and children carried through Joined/ResolvedJoin) hold the
        // materialized child built from the decrypted event and bypass the root-snapshot encryption above.
        // The read path descends into arrays and decrypts per element, so these child payloads must be
        // encrypted symmetrically on write — otherwise child-element PII is persisted in the clear and then
        // fails to release on read.
        await EncryptComplianceForChildren(schema, identifier, context.Changeset.Changes);

        return context;
    }

    async Task EncryptComplianceForChildren(JsonSchema schema, string identifier, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case ChildAdded { Child: ExpandoObject child } childAdded:
                    await EncryptComplianceForChild(schema, childAdded.ChildrenProperty, identifier, child);
                    break;

                case Joined joined:
                    await EncryptComplianceForChildren(schema, identifier, joined.Changes);
                    break;

                case ResolvedJoin resolvedJoin:
                    await EncryptComplianceForChildren(schema, identifier, resolvedJoin.Changes);
                    break;
            }
        }
    }

    async Task EncryptComplianceForChild(JsonSchema schema, PropertyPath childrenProperty, string identifier, ExpandoObject child)
    {
        var childSchema = schema.GetSchemaForPropertyPath(childrenProperty);
        if (childSchema?.HasComplianceMetadata() != true)
        {
            return;
        }

        var encryptedChild = await readModelsCompliance.Apply(eventStore, eventStoreNamespace, childSchema, identifier, child);

        // Apply writes the document compliance subject (__subject) into the result; a child element lives
        // under the root document's subject and must not carry its own, so strip it before merging back.
        var encryptedValues = (IDictionary<string, object?>)encryptedChild;
        encryptedValues.Remove(WellKnownProperties.Subject);

        // The MongoDB sink reads the child object directly when building the $push, so overwrite the child
        // in place with its encrypted values rather than replacing the change in the changeset.
        var childValues = (IDictionary<string, object?>)child;
        childValues.Clear();
        foreach (var (key, value) in encryptedValues)
        {
            childValues[key] = value;
        }
    }
}
