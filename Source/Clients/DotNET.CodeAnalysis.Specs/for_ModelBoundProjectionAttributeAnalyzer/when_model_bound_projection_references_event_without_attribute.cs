// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Xunit;
using static Cratis.Chronicle.CodeAnalysis.Specs.AnalyzerTestHelpers;

namespace Cratis.Chronicle.CodeAnalysis.Specs.for_ModelBoundProjectionAttributeAnalyzer;

public class when_model_bound_projection_references_event_without_attribute
{
    [Fact]
    public async Task should_report_diagnostic_for_from_event_attribute()
    {
        var code = @"
using Cratis.Chronicle.Projections.ModelBound;

namespace TestNamespace
{
    public record ItemAdded(string Name);

    [FromEvent<ItemAdded>]
    public class CartReadModel
    {
        public string Name { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ModelBoundProjectionAttributeAnalyzer>(code);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.ModelBoundProjectionEventTypeMustHaveAttribute, diagnostics[0].Id);
    }

    [Fact]
    public async Task should_not_report_diagnostic_when_event_has_attribute()
    {
        var code = @"
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Concepts.Events;

namespace TestNamespace
{
    [EventType]
    public record ItemAdded(string Name);

    [FromEvent<ItemAdded>]
    public class CartReadModel
    {
        public string Name { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ModelBoundProjectionAttributeAnalyzer>(code);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task should_report_diagnostic_for_removed_with_attribute()
    {
        var code = @"
using Cratis.Chronicle.Projections.ModelBound;

namespace TestNamespace
{
    public record ItemRemoved(string Id);

    [RemovedWith<ItemRemoved>]
    public class CartReadModel
    {
        public string Id { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<ModelBoundProjectionAttributeAnalyzer>(code);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.ModelBoundProjectionEventTypeMustHaveAttribute, diagnostics[0].Id);
    }
}
