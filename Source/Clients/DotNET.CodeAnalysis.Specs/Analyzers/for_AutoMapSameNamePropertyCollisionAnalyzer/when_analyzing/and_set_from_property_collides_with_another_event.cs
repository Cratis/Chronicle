// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AutoMapSameNamePropertyCollisionAnalyzer.when_analyzing;

public class and_set_from_property_collides_with_another_event : given.an_auto_map_same_name_property_collision_analyzer
{
    const string Usage = """
    public record Opened(Guid Id, string Name);
    public record Renamed(Guid Id, string Name);

    [FromEvent<Opened>]
    [FromEvent<Renamed>]
    public record Account(
        Guid Id,
        {|#0:[SetFrom<Opened>(nameof(Opened.Name))] string Name|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AutoMapSameNamePropertyCollisionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.AutoMapSameNamePropertyCollision, DiagnosticSeverity.Warning, "Name", "Renamed"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
