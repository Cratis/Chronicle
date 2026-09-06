// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationForAnalyzer.given;

public class an_event_type_generation_for_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle.Events;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "        public EventTypeAttribute(string id = \"\", uint generation = 1) { }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public abstract class EventTypeGenerationForAttribute : Attribute",
            "    {",
            "        protected EventTypeGenerationForAttribute(uint generation) { }",
            "        public abstract Type EventTypeClrType { get; }",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeGenerationForAttribute<TEventType> : EventTypeGenerationForAttribute",
            "    {",
            "        public EventTypeGenerationForAttribute(uint generation) : base(generation) { }",
            "        public override Type EventTypeClrType => typeof(TEventType);",
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
