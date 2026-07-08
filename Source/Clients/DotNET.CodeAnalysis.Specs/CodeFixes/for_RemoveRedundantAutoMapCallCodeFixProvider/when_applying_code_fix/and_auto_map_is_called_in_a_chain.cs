// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveRedundantAutoMapCallCodeFixProvider.when_applying_code_fix;

public class and_auto_map_is_called_in_a_chain : given.a_remove_redundant_auto_map_call_code_fix_provider
{
    const string Usage = """
    public class AnEvent { public string Name { get; set; } }
    public class ReadModel { public string Name { get; set; } }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) => builder.From<AnEvent>(){|#0:.AutoMap()|};
    }
    """;

    const string FixedUsage = """
    public class AnEvent { public string Name { get; set; } }
    public class ReadModel { public string Name { get; set; } }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) => builder.From<AnEvent>();
    }
    """;

    Task _result;

    void Because() => _result = CodeFixVerifier<CodeAnalysis.Analyzers.RedundantAutoMapCallAnalyzer, CodeAnalysis.CodeFixes.RemoveRedundantAutoMapCallCodeFixProvider>.VerifyCodeFix(
        CreateSource(Usage),
        CreateSource(FixedUsage),
        new ExpectedDiagnostic(DiagnosticIds.RedundantAutoMapCall, DiagnosticSeverity.Warning));

    [Fact] Task should_remove_the_redundant_auto_map_call() => _result;
}
