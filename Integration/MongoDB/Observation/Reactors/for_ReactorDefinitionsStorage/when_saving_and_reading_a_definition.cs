// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using context = Cratis.Chronicle.MongoDB.Integration.Observation.Reactors.for_ReactorDefinitionsStorage.when_saving_and_reading_a_definition.context;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.Reactors.for_ReactorDefinitionsStorage;

[Collection(MongoDBCollection.Name)]
public class when_saving_and_reading_a_definition(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_reactor_definitions_storage(fixture)
    {
        public ReactorDefinition OriginalDefinition = default!;
        public IEnumerable<ReactorDefinition> AllDefinitions = default!;
        public ReactorDefinition RetrievedDefinition = default!;
        public bool HasDefinition;

        void Establish()
        {
            OriginalDefinition = CreateReactorDefinition("MyNamespace.MyReactor");
        }

        async Task Because()
        {
            await _storage.Save(OriginalDefinition);
            AllDefinitions = await _storage.GetAll();
            RetrievedDefinition = await _storage.Get(OriginalDefinition.Identifier);
            HasDefinition = await _storage.Has(OriginalDefinition.Identifier);
        }
    }

    [Fact] void should_have_one_definition() => Context.AllDefinitions.Count().ShouldEqual(1);
    [Fact] void should_have_the_definition_with_correct_identifier() => Context.AllDefinitions.Single().Identifier.ShouldEqual(Context.OriginalDefinition.Identifier);
    [Fact] void should_retrieve_definition_by_id_with_correct_identifier() => Context.RetrievedDefinition.Identifier.ShouldEqual(Context.OriginalDefinition.Identifier);
    [Fact] void should_confirm_definition_exists() => Context.HasDefinition.ShouldBeTrue();
}
