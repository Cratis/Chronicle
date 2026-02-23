// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.InProcess.Integration.Patches.for_RenameReactors.when_applying_up.context;

namespace Cratis.Chronicle.InProcess.Integration.Patches.for_RenameReactors;

[Collection(ChronicleCollection.Name)]
public class when_applying_up(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public string TestPrefix = default!;
        public ReactorId ReactorWithGrainsId = default!;
        public ReactorId ReactorWithoutGrainsId = default!;
        public ReactorId ExpectedRenamedId = default!;
        public bool HasOriginalWithGrainsAfterPatch;
        public bool HasRenamedAfterPatch;
        public bool HasReactorWithoutGrainsAfterPatch;

        IReactorDefinitionsStorage _reactorStorage = default!;
        RenameReactors _patch = default!;

        async Task Establish()
        {
            TestPrefix = $"TestPatch{Guid.NewGuid():N}";
            ReactorWithGrainsId = new ReactorId($"{TestPrefix}.Grains.MyReactor");
            ReactorWithoutGrainsId = new ReactorId($"{TestPrefix}.OtherReactor");
            ExpectedRenamedId = new ReactorId($"{TestPrefix}.MyReactor");

            var storage = Services.GetRequiredService<IStorage>();
            _reactorStorage = storage.GetEventStore(ConceptsEventStoreName.System).Reactors;

            await _reactorStorage.Save(new ReactorDefinition(ReactorWithGrainsId, ReactorOwner.Kernel, EventSequenceId.Log, []));
            await _reactorStorage.Save(new ReactorDefinition(ReactorWithoutGrainsId, ReactorOwner.Kernel, EventSequenceId.Log, []));

            var logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger<RenameReactors>();
            _patch = new RenameReactors(storage, logger);
        }

        async Task Because()
        {
            await _patch.Up();
            HasOriginalWithGrainsAfterPatch = await _reactorStorage.Has(ReactorWithGrainsId);
            HasRenamedAfterPatch = await _reactorStorage.Has(ExpectedRenamedId);
            HasReactorWithoutGrainsAfterPatch = await _reactorStorage.Has(ReactorWithoutGrainsId);
        }

        async Task Destroy()
        {
            await _reactorStorage.Delete(ExpectedRenamedId);
            await _reactorStorage.Delete(ReactorWithoutGrainsId);
        }
    }

    [Fact] void should_not_have_the_original_reactor_with_grains_in_its_name() => Context.HasOriginalWithGrainsAfterPatch.ShouldBeFalse();
    [Fact] void should_have_the_reactor_with_its_new_renamed_id() => Context.HasRenamedAfterPatch.ShouldBeTrue();
    [Fact] void should_still_have_the_reactor_that_had_no_grains_in_its_name() => Context.HasReactorWithoutGrainsAfterPatch.ShouldBeTrue();
}
