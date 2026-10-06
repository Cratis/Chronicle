// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_multiple_subjects_contribute_to_one_model : given.a_projection_grain_with_a_child_projection
{
    IDictionary<string, object?> _collectionState;
    IDictionary<string, object?> _sessionState;
    AppendedEvent[] _events;

    void Establish()
    {
        RootOperationType = ProjectionOperationType.From;
        _events = [Contribution("name", "owner-one"), Contribution("secret", "owner-two"), Contribution("tick", "tick-owner")];
        ProjectRootWith(context => context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
            [new PropertyDifference(((IDictionary<string, object?>)context.Event.Content)["property"]!.ToString()!, null, "value")])));
    }

    async Task Because()
    {
        _collectionState = (await ProcessForMultipleReadModels(_events)).Single();
        var first = await _grain.ProcessForSingleReadModel(Concepts.EventStoreNamespaceName.Default, new ExpandoObject(), [_events[0]]);
        _sessionState = await _grain.ProcessForSingleReadModel(Concepts.EventStoreNamespaceName.Default, first, _events[1..]);
    }

    AppendedEvent Contribution(string property, string subject)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["property"] = property;
        return _event with { Context = _event.Context with { Subject = (Subject)subject }, Content = content };
    }

    [Fact] void should_keep_the_first_subject_as_the_collection_default() => _collectionState[WellKnownProperties.Subject].ShouldEqual("owner-one");
    [Fact] void should_retain_the_second_property_subject_in_the_collection() => ReadModelSubjects.From(_collectionState[WellKnownProperties.Subjects])["secret"].ShouldEqual("owner-two");
    [Fact] void should_not_replace_the_session_default_with_the_latest_event_subject() => _sessionState[WellKnownProperties.Subject].ShouldEqual("owner-one");
    [Fact] void should_carry_property_lineage_across_session_folds() => ReadModelSubjects.From(_sessionState[WellKnownProperties.Subjects])["secret"].ShouldEqual("owner-two");
}
