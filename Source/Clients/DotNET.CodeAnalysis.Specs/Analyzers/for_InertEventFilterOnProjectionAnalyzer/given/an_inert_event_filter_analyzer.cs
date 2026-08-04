// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnProjectionAnalyzer.given;

public class an_inert_event_filter_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle;",
            "using Cratis.Chronicle.Events;",
            "using Cratis.Chronicle.Projections.ModelBound;",
            "using Cratis.Chronicle.Reactors;",
            "",
            "namespace Cratis.Chronicle",
            "{",
            "    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]",
            "    public sealed class FilterEventsByTagAttribute : Attribute",
            "    {",
            "        public FilterEventsByTagAttribute(string tag) { }",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class EventStreamTypeAttribute : Attribute",
            "    {",
            "        public EventStreamTypeAttribute(string value, bool concurrency = false) { }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class EventSourceTypeAttribute : Attribute",
            "    {",
            "        public EventSourceTypeAttribute(string value, bool concurrency = false) { }",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Projections.ModelBound",
            "{",
            "    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]",
            "    public sealed class FromEventAttribute<TEvent> : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Reactors",
            "{",
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
