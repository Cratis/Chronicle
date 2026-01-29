// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers;

public class when_loading_analyzers
{
    [Fact]
    public void should_have_event_sequence_append_analyzer()
    {
        var analyzer = new EventSequenceAppendAnalyzer();
        Assert.NotNull(analyzer);
        Assert.NotEmpty(analyzer.SupportedDiagnostics);
    }

    [Fact]
    public void should_have_model_bound_projection_attribute_analyzer()
    {
        var analyzer = new ModelBoundProjectionAttributeAnalyzer();
        Assert.NotNull(analyzer);
        Assert.NotEmpty(analyzer.SupportedDiagnostics);
    }

    [Fact]
    public void should_have_reactor_method_analyzer()
    {
        var analyzer = new ReactorMethodAnalyzer();
        Assert.NotNull(analyzer);
        Assert.NotEmpty(analyzer.SupportedDiagnostics);
    }

    [Fact]
    public void should_have_reducer_method_analyzer()
    {
        var analyzer = new ReducerMethodAnalyzer();
        Assert.NotNull(analyzer);
        Assert.NotEmpty(analyzer.SupportedDiagnostics);
    }

    [Fact]
    public void all_analyzers_should_have_diagnostic_analyzer_attribute()
    {
        var assembly = typeof(EventSequenceAppendAnalyzer).Assembly;
        var analyzerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t));

        foreach (var analyzerType in analyzerTypes)
        {
            var attribute = analyzerType.GetCustomAttribute<DiagnosticAnalyzerAttribute>();
            Assert.NotNull(attribute);
        }
    }
}
