// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_DefinitionEvolutionPolicy.when_deciding_automatic_work;

public class and_policy_is_automatic : Specification
{
    bool _partialReplay;
    bool _fullReplay;

    void Because()
    {
        _partialReplay = DefinitionEvolutionPolicy.Automatic.Allows(DefinitionEvolutionOperation.PartialReplay);
        _fullReplay = DefinitionEvolutionPolicy.Automatic.Allows(DefinitionEvolutionOperation.FullReplay);
    }

    [Fact] void should_allow_partial_replay() => _partialReplay.ShouldBeTrue();
    [Fact] void should_allow_full_replay() => _fullReplay.ShouldBeTrue();
}
