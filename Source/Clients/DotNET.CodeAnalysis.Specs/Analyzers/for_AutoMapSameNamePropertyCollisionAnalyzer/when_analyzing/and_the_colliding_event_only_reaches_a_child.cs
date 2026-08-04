// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AutoMapSameNamePropertyCollisionAnalyzer.when_analyzing;

/// <summary>
/// An event named by <c>[ChildrenFrom]</c> is subscribed by the child, not by the type carrying the attribute -
/// the builder writes it into the child's definition and never into the root's. So it cannot auto-map over a
/// root property of the same name, and reporting one is a false positive.
/// </summary>
/// <remarks>
/// The cost of getting this wrong is specific: the reported fix is to fence the root property with
/// <c>[NoAutoMap]</c>, which does nothing there because nothing was overwriting it. An author following the
/// diagnostic ends up with an attribute that reads as load-bearing and is not.
/// </remarks>
public class and_the_colliding_event_only_reaches_a_child : given.an_auto_map_same_name_property_collision_analyzer
{
    const string Usage = """
    public record Opened(Guid Id, string Location);
    public record LineAdded(Guid LineId, string Location);

    public record Line(Guid LineId, string Location);

    [FromEvent<Opened>]
    public record Order(
        Guid Id,
        [SetFrom<Opened>(nameof(Opened.Location))] string Location,
        [ChildrenFrom<LineAdded>(nameof(LineAdded.LineId))] Line[] Lines);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AutoMapSameNamePropertyCollisionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_a_collision_on_the_root() => _result;
}
