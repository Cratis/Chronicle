// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Patching.for_PatchingSiloBuilderExtensions;

public class when_adding_chronicle_patching : Specification
{
    ServiceProvider _services;
    ICanApplyPatch[] _patches;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IStorage>());
        services.AddSingleton(Substitute.For<IGrainFactory>());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        silo.AddChroniclePatching();
        _services = services.BuildServiceProvider();
    }

    void Because() => _patches = [.. _services.GetServices<ICanApplyPatch>()];

    void Destroy() => _services.Dispose();

    [Fact] void should_share_rebuild_constraint_indexes() => ShouldShare<RebuildConstraintIndexes>();
    [Fact] void should_share_rebuild_constraint_indexes_for_decrypted_values() => ShouldShare<RebuildConstraintIndexesForDecryptedValues>();
    [Fact] void should_share_record_subjects_on_unique_constraint_claims() => ShouldShare<RecordSubjectsOnUniqueConstraintClaims>();
    [Fact] void should_share_remove_unhashed_unique_constraint_values() => ShouldShare<RemoveUnhashedUniqueConstraintValues>();

    void ShouldShare<TPatch>()
        where TPatch : ICanApplyPatch => _patches.OfType<TPatch>().Single().ShouldBeSame(_services.GetRequiredService<TPatch>());
}
