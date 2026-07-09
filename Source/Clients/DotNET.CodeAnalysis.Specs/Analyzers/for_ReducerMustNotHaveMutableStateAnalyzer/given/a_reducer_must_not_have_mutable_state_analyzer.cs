// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMustNotHaveMutableStateAnalyzer.given;

public class a_reducer_must_not_have_mutable_state_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Threading.Tasks;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Reducers",
            "{",
            "    public interface IReducer { }",
            "    public interface IReducerFor<TReadModel> : IReducer { }",
            "}",
            "",
            "namespace MongoDB.Driver",
            "{",
            "    public interface IMongoCollection<T> { }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
