// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_RedundantAutoMapCallAnalyzer.when_analyzing_projection_builder_methods;

public class and_parent_reenables_before_child : given.a_redundant_auto_map_call_analyzer
{
    const string Usage = """
    public class Child { public string Name { get; set; } }
    public class ReadModel { public Child[] Children { get; set; } }
    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) =>
            builder.NoAutoMap().AutoMap().Children(m => m.Children, child => child.AutoMap());
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.RedundantAutoMapCallAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage));

    [Fact] Task should_not_report_when_parent_disables_auto_map() => _result;
}
