// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AmbiguousEventStreamIdAnalyzer.given;

public class an_ambiguous_event_stream_id_analyzer : Specification
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
            "    public interface ICanProvideEventStreamId",
            "    {",
            "        string GetEventStreamId();",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]",
            "    public sealed class EventStreamIdAttribute : Attribute",
            "    {",
            "        public EventStreamIdAttribute(string value = null, bool concurrency = false) { }",
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
