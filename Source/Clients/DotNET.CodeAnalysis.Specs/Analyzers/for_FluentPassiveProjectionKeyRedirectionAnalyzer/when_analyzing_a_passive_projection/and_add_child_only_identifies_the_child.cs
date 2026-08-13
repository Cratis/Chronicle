// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer.when_analyzing_a_passive_projection;

/// <summary>
/// An AddChild callback keys the child, never the stored document a passive read resolves.
/// </summary>
public class and_add_child_only_identifies_the_child : given.a_fluent_passive_projection_key_redirection_analyzer
{
    const string Usage = """
    public record LineAdded(string OrderId, string Sku);

    public record OrderLine(string Sku);

    [Passive]
    public record Order(
        [Key] string Id,
        IEnumerable<OrderLine> Lines);

    public class OrderProjection : IProjectionFor<Order>
    {
        public void Define(IProjectionBuilderFor<Order> builder) => builder
            .From<LineAdded>(_ => _
                .AddChild(m => m.Lines, c => c
                    .UsingKey(e => e.Sku)
                    .FromObject(e => new OrderLine(e.Sku))));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
