// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorEventStoreAndEventSequenceAnalyzer.given;

public class a_reactor_event_store_and_event_sequence_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle.Events;",
            "using Cratis.Chronicle.EventSequences;",
            "using Cratis.Chronicle.Reactors;",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]",
            "    public sealed class EventStoreAttribute(string eventStore) : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.EventSequences",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public class EventSequenceAttribute(string sequence) : Attribute { }",
            "",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventLogAttribute() : EventSequenceAttribute(\"event-log\") { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Reactors",
            "{",
            "    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]",
            "    public sealed class ReactorAttribute(string id = \"\", string? eventSequence = default) : Attribute { }",
            "",
            "    public interface IReactor",
            "    {",
            "    }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}