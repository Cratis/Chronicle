// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AutoMapSameNamePropertyCollisionAnalyzer.when_analyzing;

/// <summary>
/// A property whose only same-named event is the event it is explicitly sourced from is not a collision —
/// AutoMap and the explicit setter agree on the value — so the informational diagnostic must not fire.
/// </summary>
public class and_collision_is_only_with_the_explicit_source_event : given.an_auto_map_same_name_property_collision_analyzer
{
    const string Usage = """
    public record Opened(Guid Id, string Name);

    [FromEvent<Opened>]
    public record Account(
        Guid Id,
        [SetFrom<Opened>(nameof(Opened.Name))] string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AutoMapSameNamePropertyCollisionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
