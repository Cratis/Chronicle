// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveRedundantSetToCodeFixProvider.when_applying_code_fix;

public class and_redundant_mapping_is_in_a_chain : given.a_remove_redundant_set_to_code_fix_provider
{
    const string Usage = """
    public class AnEvent { public string Name { get; set; } public string Description { get; set; } }
    public class ReadModel { public string Name { get; set; } public string Title { get; set; } }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) => builder.From<AnEvent>(evt => evt{|#0:.Set(x => x.Name).To(e => e.Name)|}.Set(x => x.Title).To(e => e.Description));
    }
    """;

    const string FixedUsage = """
    public class AnEvent { public string Name { get; set; } public string Description { get; set; } }
    public class ReadModel { public string Name { get; set; } public string Title { get; set; } }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) => builder.From<AnEvent>(evt => evt.Set(x => x.Title).To(e => e.Description));
    }
    """;

    Task _result;

    void Because() => _result = CodeFixVerifier<CodeAnalysis.Analyzers.RedundantSetToWithMatchingNamesAnalyzer, CodeAnalysis.CodeFixes.RemoveRedundantSetToCodeFixProvider>.VerifyCodeFix(
        CreateSource(Usage),
        CreateSource(FixedUsage),
        new ExpectedDiagnostic(DiagnosticIds.RedundantSetToWithMatchingNames, DiagnosticSeverity.Warning, "Name"));

    [Fact] Task should_remove_the_redundant_mapping() => _result;
}
