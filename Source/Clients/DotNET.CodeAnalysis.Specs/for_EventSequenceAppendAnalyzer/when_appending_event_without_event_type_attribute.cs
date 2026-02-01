// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;
using static Cratis.Chronicle.CodeAnalysis.Specs.AnalyzerTestHelpers;

namespace Cratis.Chronicle.CodeAnalysis.Specs.for_EventSequenceAppendAnalyzer;

public class when_appending_event_without_event_type_attribute
{
    [Fact]
    public async Task should_report_diagnostic()
    {
        var code = @"
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    public record UserCreated(string Name);

    public class TestClass
    {
        public async Task TestMethod(IEventSequence eventSequence)
        {
            await eventSequence.Append(EventSourceId.New(), new UserCreated(""John""));
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<EventSequenceAppendAnalyzer>(code);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.EventTypeMustHaveAttributeWhenAppended, diagnostics[0].Id);
        Assert.Contains("UserCreated", diagnostics[0].GetMessage());
    }

    [Fact]
    public async Task should_not_report_diagnostic_when_event_has_attribute()
    {
        var code = @"
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Concepts.Events;

namespace TestNamespace
{
    [EventType]
    public record UserCreated(string Name);

    public class TestClass
    {
        public async Task TestMethod(IEventSequence eventSequence)
        {
            await eventSequence.Append(EventSourceId.New(), new UserCreated(""John""));
        }
    }
}";

        var diagnostics = await GetDiagnosticsAsync<EventSequenceAppendAnalyzer>(code);

        Assert.Empty(diagnostics);
    }
}
