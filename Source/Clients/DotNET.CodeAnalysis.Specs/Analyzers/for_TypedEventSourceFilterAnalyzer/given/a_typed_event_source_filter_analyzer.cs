// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_TypedEventSourceFilterAnalyzer.given;

public class a_typed_event_source_filter_analyzer : Specification
{
    protected static string CreateSource(string usage) => string.Join(Environment.NewLine,
    [
        "using System;",
        "using Cratis.Chronicle.EventSources;",
        "using Cratis.Chronicle.Events;",
        "",
        "namespace Cratis.Chronicle.EventSources",
        "{",
        "    public interface IEventSource { }",
        "    [AttributeUsage(AttributeTargets.Class)] public sealed class EventSourceAttribute : Attribute { public EventSourceAttribute(string name = null) { } }",
        "    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class EventStreamAttribute(string name) : Attribute { }",
        "    [AttributeUsage(AttributeTargets.Class)] public sealed class FromEventSourceAttribute<TSource>(string stream) : Attribute where TSource : IEventSource { }",
        "}",
        "",
        "namespace Cratis.Chronicle.Events",
        "{",
        "    [AttributeUsage(AttributeTargets.Class)] public sealed class EventSourceTypeAttribute(string value) : Attribute { }",
        "    [AttributeUsage(AttributeTargets.Class)] public sealed class EventStreamTypeAttribute(string value) : Attribute { }",
        "}",
        "",
        "namespace Sample",
        "{",
        usage,
        "}"
    ]);
}
