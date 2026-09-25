// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventSequenceAppendAnalyzer.given;

public class an_event_sequence_append_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using Cratis.Chronicle.Concepts.Events;",
            "",
            "namespace Cratis.Chronicle.Concepts.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.EventSequences",
            "{",
            "    public interface IEventSequence",
            "    {",
            "        void Append(string eventSourceId, object @event);",
            "        void AppendMany(string eventSourceId, IEnumerable<object> events, string correlationId = null);",
            "        void AppendMany(IEnumerable<EventForEventSourceId> events, string correlationId = null, IDictionary<EventSourceId, string> concurrencyScopes = null);",
            "    }",
            "",
            "    public sealed class EventSequence : IEventSequence",
            "    {",
            "        public void Append(string eventSourceId, object @event)",
            "        {",
            "        }",
            "        public void AppendMany(string eventSourceId, IEnumerable<object> events, string correlationId = null)",
            "        {",
            "        }",
            "        public void AppendMany(IEnumerable<EventForEventSourceId> events, string correlationId = null, IDictionary<EventSourceId, string> concurrencyScopes = null)",
            "        {",
            "        }",
            "    }",
            "    public class EventForEventSourceId { }",
            "    public class EventSourceId { }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
