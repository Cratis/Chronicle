// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.InProcess.Integration.Patches.for_RenameReactors.when_applying_down.context;

namespace Cratis.Chronicle.InProcess.Integration.Patches.for_RenameReactors;

[Collection(ChronicleCollection.Name)]
public class when_applying_down(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public string TestPrefix = default!;
        public ReactorId ReactorWithoutGrainsId = default!;
        public ReactorId ExpectedRestoredId = default!;
        public bool HasOriginalAfterRollback;
        public bool HasRestoredWithGrainsAfterRollback;

        IReactorDefinitionsStorage _reactorStorage = default!;
        RenameReactors _patch = default!;

        async Task Establish()
        {
            TestPrefix = $"TestPatch{Guid.NewGuid():N}";
            ReactorWithoutGrainsId = new ReactorId($"{TestPrefix}.MyReactor");
            ExpectedRestoredId = new ReactorId($"{TestPrefix}.Grains.MyReactor");

            var storage = Services.GetRequiredService<IStorage>();
            _reactorStorage = storage.GetEventStore(ConceptsEventStoreName.System).Reactors;

            await _reactorStorage.Save(new ReactorDefinition(ReactorWithoutGrainsId, ReactorOwner.Kernel, EventSequenceId.Log, []));

            var logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger<RenameReactors>();
            _patch = new RenameReactors(storage, logger);
        }

        async Task Because()
        {
            await _patch.Down();
            HasOriginalAfterRollback = await _reactorStorage.Has(ReactorWithoutGrainsId);
            HasRestoredWithGrainsAfterRollback = await _reactorStorage.Has(ExpectedRestoredId);
        }

        async Task Destroy()
        {
            await _reactorStorage.Delete(ExpectedRestoredId);
        }
    }

    [Fact] void should_not_have_the_reactor_without_grains_after_rollback() => Context.HasOriginalAfterRollback.ShouldBeFalse();
    [Fact] void should_have_the_reactor_with_grains_restored_in_its_name() => Context.HasRestoredWithGrainsAfterRollback.ShouldBeTrue();
}
