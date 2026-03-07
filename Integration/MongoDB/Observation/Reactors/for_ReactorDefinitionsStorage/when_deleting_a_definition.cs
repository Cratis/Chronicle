// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using context = Cratis.Chronicle.MongoDB.Integration.Observation.Reactors.for_ReactorDefinitionsStorage.when_deleting_a_definition.context;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.Reactors.for_ReactorDefinitionsStorage;

[Collection(MongoDBCollection.Name)]
public class when_deleting_a_definition(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_reactor_definitions_storage(fixture)
    {
        public ReactorDefinition Definition = default!;
        public bool HasBeforeDelete;
        public bool HasAfterDelete;
        public IEnumerable<ReactorDefinition> AllAfterDelete = default!;

        void Establish()
        {
            Definition = CreateReactorDefinition("MyNamespace.MyReactor");
        }

        async Task Because()
        {
            await _storage.Save(Definition);
            HasBeforeDelete = await _storage.Has(Definition.Identifier);
            await _storage.Delete(Definition.Identifier);
            HasAfterDelete = await _storage.Has(Definition.Identifier);
            AllAfterDelete = await _storage.GetAll();
        }
    }

    [Fact] void should_have_definition_before_delete() => Context.HasBeforeDelete.ShouldBeTrue();
    [Fact] void should_not_have_definition_after_delete() => Context.HasAfterDelete.ShouldBeFalse();
    [Fact] void should_have_no_definitions_after_delete() => Context.AllAfterDelete.ShouldBeEmpty();
}
