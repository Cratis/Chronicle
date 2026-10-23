// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_ContractComparison.when_comparing;

/// <summary>
/// The change #5012 should have made: keep Register as it was and add a new method beside it.
/// </summary>
public class and_a_release_added_a_method_and_raised_the_version : Specification
{
    const string Before = """
        class [GrainType("projectionsmanager")] Cratis.Chronicle.Projections.ProjectionsManager

        interface [GrainInterfaceType("Cratis.Chronicle.Projections.IProjectionsManager")] Cratis.Chronicle.Projections.IProjectionsManager [Version(0)]
          6E2D44AB: Register(System.Collections.Generic.IEnumerable<Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition>, Cratis.Chronicle.Concepts.Projections.ProjectionOwner?, string?) -> Task
        """;

    const string After = """
        class [GrainType("projectionsmanager")] Cratis.Chronicle.Projections.ProjectionsManager

        interface [GrainInterfaceType("Cratis.Chronicle.Projections.IProjectionsManager")] Cratis.Chronicle.Projections.IProjectionsManager [Version(1)]
          6E2D44AB: Register(System.Collections.Generic.IEnumerable<Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition>, Cratis.Chronicle.Concepts.Projections.ProjectionOwner?, string?) -> Task
          0A1B2C3D: RegisterWithVersion(System.Collections.Generic.IEnumerable<Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition>, Cratis.Chronicle.Concepts.Projections.ProjectionOwner?, string?, string?) -> Task
        """;

    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = ContractComparison.Compare(ContractManifest.Parse(Before), ContractManifest.Parse(After));

    [Fact] void should_find_nothing_that_breaks() => _breaks.ShouldBeEmpty();
}
