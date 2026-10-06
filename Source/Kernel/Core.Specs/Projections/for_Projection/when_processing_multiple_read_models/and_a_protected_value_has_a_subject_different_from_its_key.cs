// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_protected_value_has_a_subject_different_from_its_key : given.a_projection_grain_with_a_child_projection
{
    const string Owner = "owner-not-the-read-model-key";
    IDictionary<string, object?> _result;

    void Establish()
    {
        RootOperationType = ProjectionOperationType.From;
        _event = _event with { Context = _event.Context with { Subject = new Subject(Owner) } };
        ProjectRootWith(context => context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
        [
            new PropertyDifference("name", null, "encrypted-name")
        ])));
    }

    async Task Because() => _result = (await ProcessForMultipleReadModels(_event)).Single();

    [Fact] void should_preserve_the_projected_value() => _result["name"].ShouldEqual("encrypted-name");
    [Fact] void should_retain_authoritative_subject_lineage() =>
        ((_result.TryGetValue(WellKnownProperties.Subject, out var subject) && subject?.ToString() == Owner) ||
         (_result.TryGetValue(WellKnownProperties.Subjects, out var subjects) && ReadModelSubjects.From(subjects).GetValueOrDefault("name") == Owner)).ShouldBeTrue();
}
