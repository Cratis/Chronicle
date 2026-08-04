// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asking_which_layers_the_harness_substitutes;

/// <summary>
/// A class-level <c>[RemovedWith]</c> deletes the document in the real sink; here it is modeled by dropping
/// the in-memory state. A spec can assert the read model came back null either way, which is exactly why the
/// scenario has to say that the delete itself was not the one that ran.
/// </summary>
public class and_the_read_model_is_removed_at_the_root : Specification
{
    ReadModelScenario<RemovableWidget> _scenario;
    IReadOnlyList<ReadModelSubstitution> _substitutions;

    void Establish() => _scenario = new ReadModelScenario<RemovableWidget>();

    void Because() => _substitutions = _scenario.Substitutions;

    [Fact] void should_report_the_sink() => _substitutions.Select(_ => _.Layer).ShouldContain(ReadModelSubstitutedLayer.Sink);
    [Fact] void should_name_the_root_removal() => _substitutions.Single().Shape.ShouldContain("[RemovedWith]");
}
