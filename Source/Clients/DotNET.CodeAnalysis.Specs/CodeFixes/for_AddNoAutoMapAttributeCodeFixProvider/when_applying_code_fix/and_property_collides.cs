// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AddNoAutoMapAttributeCodeFixProvider.when_applying_code_fix;

public class and_property_collides : given.an_add_no_auto_map_attribute_code_fix_provider
{
    const string Usage = """
    public record Opened(Guid Id, string Name);
    public record Renamed(Guid Id, string Name);
    [FromEvent<Opened>][FromEvent<Renamed>] public record Account(Guid Id, {|#0:[SetFrom<Opened>(nameof(Opened.Name))] string Name|});
    """;

    const string FixedUsage = """
    public record Opened(Guid Id, string Name);
    public record Renamed(Guid Id, string Name);
    [FromEvent<Opened>][FromEvent<Renamed>] public record Account(Guid Id, [SetFrom<Opened>(nameof(Opened.Name))][NoAutoMap] string Name);
    """;

    Task _result;

    void Because() => _result = CodeFixVerifier<CodeAnalysis.Analyzers.AutoMapSameNamePropertyCollisionAnalyzer, CodeAnalysis.CodeFixes.AddNoAutoMapAttributeCodeFixProvider>.VerifyCodeFix(
        CreateSource(Usage),
        CreateSource(FixedUsage).Replace(
            "using Cratis.Chronicle.Projections.ModelBound;",
            "using Cratis.Chronicle.Projections.ModelBound;" + Environment.NewLine + "using Cratis.Chronicle.Projections;",
            StringComparison.Ordinal),
        new ExpectedDiagnostic(DiagnosticIds.AutoMapSameNamePropertyCollision, DiagnosticSeverity.Warning, "Name", "Renamed"));

    [Fact] Task should_add_the_no_auto_map_attribute() => _result;
}
