// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Concepts;
using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_a_concept_collection.and_the_concept_collection_persists.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_a_concept_collection;

[Collection(ChronicleCollection.Name)]
public class and_the_concept_collection_persists(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<ConceptTagsProjection, ConceptTagsReadModel>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(ConceptTagsAssigned)];

        void Establish() =>
            EventsToAppend.Add(new ConceptTagsAssigned([new StringConcept("React"), new StringConcept("TypeScript")]));
    }

    [Fact] void should_materialize_the_instance() => Context.Result.ShouldNotBeNull();
    [Fact] void should_persist_the_whole_concept_collection() => Context.Result.Tags.Count.ShouldEqual(2);
    [Fact] void should_persist_the_first_concept_value() => Context.Result.Tags.Select(_ => _.Value).ShouldContain("React");
    [Fact] void should_persist_the_second_concept_value() => Context.Result.Tags.Select(_ => _.Value).ShouldContain("TypeScript");
}
