// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMultipleEventStoresAnalyzer.given;

public class a_reactor_multiple_event_stores_analyzer : Specification
{
    protected static string CreateSource(string usage) => CreateSource(usage, assemblyAttributes: string.Empty);

    protected static string CreateSource(string usage, string assemblyAttributes)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Threading.Tasks;",
            "using Cratis.Chronicle.Concepts.Events;",
            "using Cratis.Chronicle.Events;",
            "",
            assemblyAttributes,
            "",
            "namespace Cratis.Chronicle.Concepts.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]",
            "    public sealed class EventStoreAttribute : Attribute",
            "    {",
            "        public EventStoreAttribute(string eventStore) { }",
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
