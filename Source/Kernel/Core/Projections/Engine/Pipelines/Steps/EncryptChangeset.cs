// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
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

        var schema = projection.TargetReadModelSchema;
        var identifier = context.Event.Context.ResolveComplianceIdentifier(context.Key);

        // A read model whose schema graph declares no compliance metadata has no PII to encrypt.
        // Applying compliance would return the state unchanged, so the whole-document self-compare and
        // per-member re-encryption below would walk the entire document — children included — only to
        // find nothing. Skip that work entirely and write the compliance subject directly. This mirrors
        // the gate DecryptInitialState already applies on the read path, keeping write and read symmetric.
        if (!schema.HasComplianceMetadata())
        {
            SetSubjectWithoutCompliance(context, identifier);
            return context;
        }

        var currentState = context.Changeset.CurrentState;
        var currentStateAsDictionary = (IDictionary<string, object?>)currentState;
        currentStateAsDictionary.TryGetValue(WellKnownProperties.Subject, out var currentSubjectValue);
        var currentSubject = currentSubjectValue as string;
        var defaultSubject = currentSubject ?? identifier;
        currentStateAsDictionary.TryGetValue(WellKnownProperties.Subjects, out var currentSubjectsValue);
        var currentSubjects = ReadModelSubjects.From(currentSubjectsValue);
        var updatedSubjects = new Dictionary<string, string>(currentSubjects, StringComparer.Ordinal);

        var eventSubject = context.IsJoin
            ? SubjectFor(context.Event)
            : identifier;
        UpdateSubjectsForChanges(schema, context.Changeset.Changes, eventSubject, defaultSubject, updatedSubjects);

        var subjectsChanged = !SubjectsEqual(currentSubjects, updatedSubjects);
        if (updatedSubjects.Count > 0)
        {
            currentStateAsDictionary[WellKnownProperties.Subjects] = ReadModelSubjects.ToExpandoObject(updatedSubjects);
        }
        else
        {
            currentStateAsDictionary.Remove(WellKnownProperties.Subjects);
        }

        var encrypted = await readModelsCompliance.Apply(
            eventStore,
            eventStoreNamespace,
            schema,
            identifier,
            currentState);

        var hasDifferences = !objectComparer.Compare(currentState, encrypted, out var differences);

        // Apply re-encrypts the whole snapshot, so the comparer reports a difference for every PII member —
        // including members inside child collections (e.g. contacts.contactEmail). Those nested differences
        // carry no array indexers, so collapse them into a single whole-collection replacement; otherwise the
        // sink emits a non-positional dotted $set that MongoDB rejects with WriteError Code 28. This mirrors
        // the guarantee the reducer pipeline already relies on for reducer-owned collections.
        var propertyDifferences = hasDifferences && differences is not null
            ? differences.Collapse(currentState, encrypted).ToList()
            : [];

        var joinedComplianceProperties = GetJoinedComplianceProperties(schema, context.Changeset.Changes).ToHashSet(StringComparer.OrdinalIgnoreCase);
        propertyDifferences.RemoveAll(_ =>
            _.PropertyPath.Segments.FirstOrDefault()?.Value is string rootProperty &&
            joinedComplianceProperties.Contains(rootProperty));
        await EncryptJoinedComplianceValues(schema, eventSubject, context.Changeset.Changes);

        if (subjectsChanged)
        {
            foreach (var property in currentSubjects.Keys.Concat(updatedSubjects.Keys).Distinct(StringComparer.Ordinal))
            {
                currentSubjects.TryGetValue(property, out var currentPropertySubject);
                updatedSubjects.TryGetValue(property, out var updatedPropertySubject);
                if (currentPropertySubject == updatedPropertySubject)
                {
                    continue;
                }

                propertyDifferences.Add(new PropertyDifference(
                    new PropertyPath($"{WellKnownProperties.Subjects}.{property}"),
                    currentPropertySubject,
                    updatedPropertySubject));
            }
        }

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

    static void UpdateSubjectsForChanges(
        JsonSchema schema,
        IEnumerable<Change> changes,
        string subject,
        string defaultSubject,
        IDictionary<string, string> subjects)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    foreach (var difference in propertiesChanged.Differences)
                    {
                        UpdateSubjectForProperty(schema, difference.PropertyPath, subject, defaultSubject, subjects);
                    }
                    break;

                case ChildAdded childAdded:
                    UpdateSubjectForProperty(schema, childAdded.ChildrenProperty, subject, defaultSubject, subjects);
                    break;

                case NestedCleared nestedCleared:
                    UpdateSubjectForProperty(schema, nestedCleared.NestedProperty, subject, defaultSubject, subjects);
                    break;

                case Joined joined:
                    UpdateSubjectsForChanges(schema, joined.Changes, subject, defaultSubject, subjects);
                    break;

                case ResolvedJoin resolvedJoin:
                    var resolvedSubject = resolvedJoin.Source is AppendedEvent resolvedEvent
                        ? SubjectFor(resolvedEvent)
                        : subject;
                    UpdateSubjectsForChanges(schema, resolvedJoin.Changes, resolvedSubject, defaultSubject, subjects);
                    break;
            }
        }
    }

    static void UpdateSubjectForProperty(
        JsonSchema schema,
        PropertyPath propertyPath,
        string subject,
        string defaultSubject,
        IDictionary<string, string> subjects)
    {
        var rootProperty = propertyPath.Segments.FirstOrDefault()?.Value;
        if (rootProperty is null ||
            !schema.Properties.TryGetValue(rootProperty, out var propertySchema) ||
            !propertySchema.HasComplianceMetadata())
        {
            return;
        }

        if (subject == defaultSubject)
        {
            subjects.Remove(rootProperty);
        }
        else
        {
            subjects[rootProperty] = subject;
        }
    }

    static string SubjectFor(AppendedEvent @event) =>
        @event.Context.Subject?.IsSet == true
            ? @event.Context.Subject.Value
            : @event.Context.EventSourceId.Value;

    static bool SubjectsEqual(Dictionary<string, string> left, Dictionary<string, string> right) =>
        left.Count == right.Count && left.All(entry => right.TryGetValue(entry.Key, out var value) && value == entry.Value);

    static IEnumerable<string> GetJoinedComplianceProperties(JsonSchema schema, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            if (change is not Joined and not ResolvedJoin)
            {
                continue;
            }

            var nestedChanges = change switch
            {
                Joined joined => joined.Changes,
                ResolvedJoin resolvedJoin => resolvedJoin.Changes,
                _ => []
            };

            foreach (var property in GetComplianceProperties(schema, nestedChanges))
            {
                yield return property;
            }
        }
    }

    static IEnumerable<string> GetComplianceProperties(JsonSchema schema, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    foreach (var rootProperty in propertiesChanged.Differences
                                 .Where(_ => IsComplianceProperty(schema, _.PropertyPath))
                                 .Select(_ => _.PropertyPath.Segments.First().Value))
                    {
                        yield return rootProperty;
                    }
                    break;

                case Joined joined:
                    foreach (var property in GetComplianceProperties(schema, joined.Changes))
                    {
                        yield return property;
                    }
                    break;

                case ResolvedJoin resolvedJoin:
                    foreach (var property in GetComplianceProperties(schema, resolvedJoin.Changes))
                    {
                        yield return property;
                    }
                    break;
            }
        }
    }

    static bool IsComplianceProperty(JsonSchema schema, PropertyPath propertyPath)
    {
        var rootProperty = propertyPath.Segments.FirstOrDefault()?.Value;
        return rootProperty is not null &&
               schema.Properties.TryGetValue(rootProperty, out var propertySchema) &&
               propertySchema.HasComplianceMetadata();
    }

    static void SetSubjectWithoutCompliance(ProjectionEventContext context, string identifier)
    {
        var currentState = context.Changeset.CurrentState;
        var currentStateAsDictionary = (IDictionary<string, object?>)currentState;
        currentStateAsDictionary.TryGetValue(WellKnownProperties.Subject, out var currentSubjectValue);
        var subjectWasAbsent = currentSubjectValue is not string;

        // Write the subject onto the state exactly as the compliance path would, so any consumer reading
        // the current state after this step sees the same value regardless of whether the model has PII.
        currentStateAsDictionary[WellKnownProperties.Subject] = identifier;

        // Record the subject as a change only when it was previously absent — a whole-collection removal
        // owns the document identity for that turn, so it must not be paired with a subject write.
        if (subjectWasAbsent && context.Changeset.Changes.All(_ => _ is not ChildRemovedFromAll))
        {
            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(
                currentState,
                [new PropertyDifference(WellKnownProperties.Subject, null, identifier)]));
        }
    }

    async Task EncryptJoinedComplianceValues(JsonSchema schema, string subject, IEnumerable<Change> changes)
    {
        if (changes is not IList<Change> mutableChanges || mutableChanges.IsReadOnly)
        {
            return;
        }

        for (var index = 0; index < mutableChanges.Count; index++)
        {
            mutableChanges[index] = mutableChanges[index] switch
            {
                Joined joined => joined with { Changes = await EncryptComplianceValues(schema, subject, joined.Changes) },
                ResolvedJoin resolvedJoin => resolvedJoin with
                {
                    Changes = await EncryptComplianceValues(
                        schema,
                        resolvedJoin.Source is AppendedEvent resolvedEvent ? SubjectFor(resolvedEvent) : subject,
                        resolvedJoin.Changes)
                },
                _ => mutableChanges[index]
            };
        }
    }

    async Task<Change[]> EncryptComplianceValues(JsonSchema schema, string subject, IEnumerable<Change> changes)
    {
        var result = new List<Change>();
        foreach (var change in changes)
        {
            result.Add(change switch
            {
                PropertiesChanged<ExpandoObject> propertiesChanged => await EncryptComplianceValues(schema, subject, propertiesChanged),
                Joined joined => joined with { Changes = await EncryptComplianceValues(schema, subject, joined.Changes) },
                ResolvedJoin resolvedJoin => resolvedJoin with
                {
                    Changes = await EncryptComplianceValues(
                        schema,
                        resolvedJoin.Source is AppendedEvent resolvedEvent ? SubjectFor(resolvedEvent) : subject,
                        resolvedJoin.Changes)
                },
                _ => change
            });
        }

        return [.. result];
    }

    async Task<PropertiesChanged<ExpandoObject>> EncryptComplianceValues(
        JsonSchema schema,
        string subject,
        PropertiesChanged<ExpandoObject> propertiesChanged)
    {
        var complianceDifferences = propertiesChanged.Differences
            .Where(_ => IsComplianceProperty(schema, _.PropertyPath))
            .ToArray();
        if (complianceDifferences.Length == 0)
        {
            return propertiesChanged;
        }

        var state = new ExpandoObject();
        foreach (var difference in complianceDifferences)
        {
            difference.PropertyPath.SetValue(state, difference.Changed!, ArrayIndexers.NoIndexers);
        }

        var encryptedState = await readModelsCompliance.Apply(
            eventStore,
            eventStoreNamespace,
            schema,
            subject,
            state);
        return propertiesChanged with
        {
            Differences = propertiesChanged.Differences
                .Select(difference => IsComplianceProperty(schema, difference.PropertyPath)
                    ? new PropertyDifference(
                        difference.PropertyPath,
                        difference.Original,
                        difference.PropertyPath.GetValue(encryptedState, difference.ArrayIndexers),
                        difference.ArrayIndexers)
                    : difference)
                .ToArray()
        };
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
        encryptedValues.Remove(WellKnownProperties.Subjects);

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
