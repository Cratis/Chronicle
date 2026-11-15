// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SinkAndPassiveCombinedAnalyzer.given;

public class a_sink_and_passive_combined_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle.ReadModels;",
            "using Cratis.Chronicle.Sinks;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.ReadModels",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class PassiveAttribute : Attribute { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Sinks",
            "{",
            "    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]",
            "    public sealed class SinkAttribute : Attribute",
            "    {",
            "        public SinkAttribute(string name) { }",
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
