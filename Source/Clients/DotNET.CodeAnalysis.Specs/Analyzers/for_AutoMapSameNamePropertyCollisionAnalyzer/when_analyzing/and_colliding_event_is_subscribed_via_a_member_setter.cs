// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AutoMapSameNamePropertyCollisionAnalyzer.when_analyzing;

/// <summary>
/// Mirrors the real <c>ArrangementSummary</c>: the colliding event is subscribed not class-level but via a
/// member-level <c>[SetFrom]</c> on a different property, and still overwrites the explicitly sourced value.
/// </summary>
public class and_colliding_event_is_subscribed_via_a_member_setter : given.an_auto_map_same_name_property_collision_analyzer
{
    const string Usage = """
    public record ArrangementSet(string Location);
    public record WorkModeSet(string WorkMode, string Location);

    [FromEvent<ArrangementSet>]
    public record Summary(
        Guid Id,
        {|#0:[SetFrom<ArrangementSet>] string Location|},
        [SetFrom<WorkModeSet>] string WorkMode);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AutoMapSameNamePropertyCollisionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.AutoMapSameNamePropertyCollision, DiagnosticSeverity.Info, "Location", "WorkModeSet"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
