// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AddEventTypeAttributeCodeFixProvider.when_applying_code_fix;

public class and_event_type_is_not_marked_with_event_type : given.an_add_event_type_attribute_code_fix_provider
{
    const string Usage = """
    public class MissingEvent
    {
    }

    public class Usage
    {
        public void Append()
        {
            var sequence = new Cratis.Chronicle.EventSequences.EventSequence();
            sequence.Append("source", {|#0:new MissingEvent()|});
        }
    }
    """;

    const string FixedUsage = """
        [EventType]
    public class MissingEvent
    {
    }

    public class Usage
    {
        public void Append()
        {
            var sequence = new Cratis.Chronicle.EventSequences.EventSequence();
            sequence.Append("source", new MissingEvent());
        }
    }
    """;

    Task _result;

    void Because() => _result = CodeFixVerifier<CodeAnalysis.Analyzers.EventSequenceAppendAnalyzer, CodeAnalysis.CodeFixes.AddEventTypeAttributeCodeFixProvider>.VerifyCodeFix(CreateEventSequenceSource(Usage), CreateEventSequenceSource(FixedUsage).Replace("using System;", "using System;\nusing Cratis.Chronicle.Concepts.Events;"), new ExpectedDiagnostic(DiagnosticIds.EventTypeMustHaveAttributeWhenAppended, DiagnosticSeverity.Error, "MissingEvent"));

    [Fact] Task should_add_event_type_attribute_to_event_type() => _result;
}
