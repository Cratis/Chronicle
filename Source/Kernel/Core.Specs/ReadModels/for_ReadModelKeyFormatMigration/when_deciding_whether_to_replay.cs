// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.ReadModels.for_ReadModelKeyFormatMigration;

public class when_deciding_whether_to_replay : Specification
{
    bool _automaticNamespaced;
    bool _automaticGlobal;
    bool _automaticBoth;
    bool _manualNamespaced;
    bool _partialOnlyNamespaced;

    void Because()
    {
        _automaticNamespaced = ReadModelKeyFormatMigration.CanReplay(DefinitionEvolutionPolicy.Automatic, ProjectionScope.Namespaced);
        _automaticGlobal = ReadModelKeyFormatMigration.CanReplay(DefinitionEvolutionPolicy.Automatic, ProjectionScope.Global);
        _automaticBoth = ReadModelKeyFormatMigration.CanReplay(DefinitionEvolutionPolicy.Automatic, ProjectionScope.Both);
        _manualNamespaced = ReadModelKeyFormatMigration.CanReplay(DefinitionEvolutionPolicy.Manual, ProjectionScope.Namespaced);
        _partialOnlyNamespaced = ReadModelKeyFormatMigration.CanReplay(DefinitionEvolutionPolicy.PartialOnly, ProjectionScope.Namespaced);
    }

    [Fact] void should_replay_a_namespaced_projection_automatically() => _automaticNamespaced.ShouldBeTrue();
    [Fact] void should_not_replay_a_global_projection() => _automaticGlobal.ShouldBeFalse();
    [Fact] void should_not_replay_a_projection_that_shares_a_global_sink() => _automaticBoth.ShouldBeFalse();
    [Fact] void should_only_recommend_under_the_manual_policy() => _manualNamespaced.ShouldBeFalse();
    [Fact] void should_only_recommend_under_the_partial_only_policy() => _partialOnlyNamespaced.ShouldBeFalse();
}
