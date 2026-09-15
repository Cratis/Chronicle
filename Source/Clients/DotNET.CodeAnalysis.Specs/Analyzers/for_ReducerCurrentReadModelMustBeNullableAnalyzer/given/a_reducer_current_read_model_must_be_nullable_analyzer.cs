// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerCurrentReadModelMustBeNullableAnalyzer.given;

public class a_reducer_current_read_model_must_be_nullable_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "#nullable enable",
            "using System;",
            "using System.Threading.Tasks;",
            "using Cratis.Chronicle.Concepts.Events;",
            "using Cratis.Chronicle.Reducers;",
            "",
            "namespace Cratis.Chronicle.Concepts.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Reducers",
            "{",
            "    public interface IReducer",
            "    {",
            "    }",
            "",
            "    public interface IReducerFor<TReadModel> : IReducer",
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
