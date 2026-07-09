// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AutoMapSameNamePropertyCollisionAnalyzer.when_analyzing;

public class and_type_has_class_level_no_auto_map : given.an_auto_map_same_name_property_collision_analyzer
{
    const string Usage = """
    public record Opened(Guid Id, string Name);
    public record Renamed(Guid Id, string Name);

    [NoAutoMap]
    [FromEvent<Opened>]
    [FromEvent<Renamed>]
    public record Account(
        Guid Id,
        [SetFrom<Opened>(nameof(Opened.Name))] string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AutoMapSameNamePropertyCollisionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
