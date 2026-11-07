// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

public partial class EncryptChangeset
{
    /// <inheritdoc/>
    public async ValueTask ProtectFutureChangeset(IProjection projection, ProjectionEventContext futureContext)
    {
        var schema = projection.TargetReadModelSchema;
        if (!schema.HasSchemaMetadata())
        {
            return;
        }

        var changeset = futureContext.Changeset;
        var identifier = futureContext.Event.Context.ResolveComplianceIdentifier(futureContext.Key);
        var eventSubject = futureContext.IsJoin ? SubjectFor(futureContext.Event) : identifier;
        var subjectDifferences = UpdateFutureSubjects(schema, futureContext, identifier, eventSubject);
        var snapshot = await readModelsCompliance.Apply(eventStore, eventStoreNamespace, schema, identifier, changeset.CurrentState);

        // Only read the future's own paths: other snapshot values may already be encrypted by the main turn.
        ProtectFutureDifferences(schema, futureContext, snapshot);
        await EncryptJoinedProtectedValues(schema, eventSubject, changeset.Changes);
        if (subjectDifferences.Count > 0)
        {
            changeset.Add(new PropertiesChanged<ExpandoObject>(changeset.CurrentState, subjectDifferences));
        }

        await EncryptProtectedValuesForChildren(schema, identifier, changeset.Changes);
    }

    static List<PropertyDifference> UpdateFutureSubjects(
        JsonSchema schema,
        ProjectionEventContext context,
        string identifier,
        string eventSubject)
    {
        var state = (IDictionary<string, object?>)context.Changeset.CurrentState;
        state.TryGetValue(WellKnownProperties.Subject, out var currentSubjectValue);
        state.TryGetValue(WellKnownProperties.Subjects, out var currentSubjectsValue);
        var currentSubjects = ReadModelSubjects.From(currentSubjectsValue);
        var updatedSubjects = new Dictionary<string, string>(currentSubjects, StringComparer.Ordinal);
        UpdateSubjectsForChanges(schema, context.Changeset.Changes, eventSubject, currentSubjectValue as string ?? identifier, updatedSubjects);
        if (updatedSubjects.Count > 0)
        {
            state[WellKnownProperties.Subjects] = ReadModelSubjects.ToExpandoObject(updatedSubjects);
        }
        else
        {
            state.Remove(WellKnownProperties.Subjects);
        }

        var differences = new List<PropertyDifference>();
        if (!SubjectsEqual(currentSubjects, updatedSubjects))
        {
            AddSubjectDifferences(currentSubjects, updatedSubjects, differences);
        }

        return differences;
    }

    static void ProtectFutureDifferences(JsonSchema schema, ProjectionEventContext context, ExpandoObject snapshot)
    {
        var changes = (IList<Change>)context.Changeset.Changes;
        for (var index = 0; index < changes.Count; index++)
        {
            if (changes[index] is PropertiesChanged<ExpandoObject> propertiesChanged)
            {
                changes[index] = propertiesChanged with
                {
                    Differences = propertiesChanged.Differences
                        .Select(difference => IsProtectedProperty(schema, difference.PropertyPath)
                            ? ProtectedFutureDifference(context, snapshot, difference)
                            : difference)
                        .ToArray()
                };
            }
        }
    }

    static PropertyDifference ProtectedFutureDifference(ProjectionEventContext context, ExpandoObject snapshot, PropertyDifference difference)
    {
        var protectedValue = difference.PropertyPath.GetValue(snapshot, difference.ArrayIndexers);
        if (difference.Changed is not null && protectedValue is null)
        {
            throw new FutureValueCouldNotBeProtected(difference.PropertyPath, context.Event.Context.SequenceNumber);
        }

        return new PropertyDifference(difference.PropertyPath, difference.Original, protectedValue, difference.ArrayIndexers);
    }
}
