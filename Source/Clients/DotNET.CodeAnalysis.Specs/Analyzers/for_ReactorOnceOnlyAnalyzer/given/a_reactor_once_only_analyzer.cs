// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorOnceOnlyAnalyzer.given;

public class a_reactor_once_only_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Collections.Generic;",
            "using System.Threading.Tasks;",
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
            "namespace Cratis.Chronicle.Events",
            "{",
            "    public sealed class EventContext",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Reactors",
            "{",
            "    public interface IReactor",
            "    {",
            "    }",
            "",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]",
            "    public sealed class OnceOnlyAttribute : Attribute",
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
