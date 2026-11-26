// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.given;

public class scenario_terminal_calls : Specification
{
    protected static readonly string[] Calls =
    [
        "reactor.Events(new object())",
        "scenario.Redeliver(new RecordedOccurrence())",
        "given.Events(new object())",
        "when.Events(new object())"
    ];

    protected static string CreateSource(string usage, string returnType = "Task", string whenReturnType = "Task<AppendResult>") => $$"""
        using System;
        using System.Threading.Tasks;
        using Cratis.Chronicle.Testing.Reactors;
        using Cratis.Chronicle.Testing.EventSequences;
        using ReactorBuilder = Cratis.Chronicle.Testing.Reactors.ReactorSourceGivenBuilder<int>;

        namespace Cratis.Chronicle.Testing.Reactors
        {
            public class RecordedOccurrence { }
            public class ReactorSourceGivenBuilder<TReactor>
            {
                public {{returnType}} Events(params object[] events) => throw new NotSupportedException();
                public Task Events<T>(params object[] events) => Task.CompletedTask;
                public Task Events(int count) => Task.CompletedTask;
            }
            public class ReactorScenario<TReactor>
            {
                public {{returnType}} Redeliver(RecordedOccurrence occurrence) => throw new NotSupportedException();
                public Task Redeliver(object occurrence) => Task.CompletedTask;
            }
        }
        namespace Cratis.Chronicle.Testing.EventSequences
        {
            public class AppendResult { }
            public class EventSourceGivenBuilder
            {
                public {{returnType}} Events(params object[] events) => throw new NotSupportedException();
                public Task Events<T>(params object[] events) => Task.CompletedTask;
            }
            public class EventSourceWhenBuilder
            {
                public {{whenReturnType}} Events(object @event, params object[] additionalEvents) => throw new NotSupportedException();
                public Task Events() => Task.CompletedTask;
            }
            public class OtherBuilder
            {
                public Task Events(params object[] events) => Task.CompletedTask;
            }
        }
        namespace Sample
        {
            public class Spec
            {
                ReactorBuilder reactor = new();
                ReactorScenario<int> scenario = new();
                EventSourceGivenBuilder given = new();
                EventSourceWhenBuilder when = new();
                Task captured;
                {{usage}}
            }
        }
        """;
}
