// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using context = Cratis.Chronicle.MongoDB.Integration.Observation.Reactors.for_ReactorDefinitionsStorage.when_renaming_a_reactor.context;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.Reactors.for_ReactorDefinitionsStorage;

[Collection(MongoDBCollection.Name)]
public class when_renaming_a_reactor(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_reactor_definitions_storage(fixture)
    {
        public ReactorId OriginalId;
        public ReactorId NewId;
        public bool HasOriginalAfterRename;
        public bool HasNewAfterRename;
        public IEnumerable<ReactorDefinition> AllAfterRename = default!;

        void Establish()
        {
            OriginalId = new ReactorId("MyNamespace.Grains.MyReactor");
            NewId = new ReactorId("MyNamespace.MyReactor");
        }

        async Task Because()
        {
            await _storage.Save(CreateReactorDefinition(OriginalId));
            await _storage.Rename(OriginalId, NewId);
            HasOriginalAfterRename = await _storage.Has(OriginalId);
            HasNewAfterRename = await _storage.Has(NewId);
            AllAfterRename = await _storage.GetAll();
        }
    }

    [Fact] void should_no_longer_have_the_original_id() => Context.HasOriginalAfterRename.ShouldBeFalse();
    [Fact] void should_have_the_new_id() => Context.HasNewAfterRename.ShouldBeTrue();
    [Fact] void should_only_have_one_reactor() => Context.AllAfterRename.Count().ShouldEqual(1);
    [Fact] void should_have_reactor_with_new_id_in_all() => Context.AllAfterRename.Single().Identifier.ShouldEqual(Context.NewId);
}
