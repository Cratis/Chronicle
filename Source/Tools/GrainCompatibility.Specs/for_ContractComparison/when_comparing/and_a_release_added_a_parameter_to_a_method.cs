// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_ContractComparison.when_comparing;

/// <summary>
/// The change that broke rolling upgrades from 19.70 to 19.106 (#5272): Register gained an optional parameter,
/// which compiles against every caller but is a different method on the wire.
/// </summary>
public class and_a_release_added_a_parameter_to_a_method : Specification
{
    const string Before = """
        interface [GrainInterfaceType("Cratis.Chronicle.Projections.IProjectionsManager")] Cratis.Chronicle.Projections.IProjectionsManager [Version(0)]
          E36EB1EC: Ensure() -> Task
          6E2D44AB: Register(System.Collections.Generic.IEnumerable<Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition>, Cratis.Chronicle.Concepts.Projections.ProjectionOwner?, string?) -> Task
        """;

    const string After = """
        interface [GrainInterfaceType("Cratis.Chronicle.Projections.IProjectionsManager")] Cratis.Chronicle.Projections.IProjectionsManager [Version(0)]
          E36EB1EC: Ensure() -> Task
          F436C56F: Register(System.Collections.Generic.IEnumerable<Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition>, Cratis.Chronicle.Concepts.Projections.ProjectionOwner?, string?, string?) -> Task
        """;

    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = ContractComparison.Compare(ContractManifest.Parse(Before), ContractManifest.Parse(After));

    [Fact] void should_report_the_previous_method_as_removed() => _breaks.ShouldContain(_ => _.Kind == ContractBreakKind.MethodRemoved && _.Description.Contains("6E2D44AB"));
    [Fact] void should_report_the_new_method_as_added_without_a_version_increase() => _breaks.ShouldContain(_ => _.Kind == ContractBreakKind.MethodsAddedWithoutVersionIncrease && _.Description.Contains("F436C56F"));
    [Fact] void should_name_the_interface() => _breaks.All(_ => _.Subject == "Cratis.Chronicle.Projections.IProjectionsManager").ShouldBeTrue();
}
