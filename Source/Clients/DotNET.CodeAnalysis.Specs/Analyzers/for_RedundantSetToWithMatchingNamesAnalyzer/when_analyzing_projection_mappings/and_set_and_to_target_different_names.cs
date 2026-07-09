// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_RedundantSetToWithMatchingNamesAnalyzer.when_analyzing_projection_mappings;

public class and_set_and_to_target_different_names : given.a_redundant_set_to_with_matching_names_analyzer
{
    const string Usage = """
    public class AnEvent { public string Title { get; set; } }
    public class ReadModel { public string Name { get; set; } }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) =>
            builder.From<AnEvent>(evt => evt.Set(x => x.Name).To(e => e.Title));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.RedundantSetToWithMatchingNamesAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
