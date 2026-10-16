// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_methods_are_helpers_or_queries : given.read_model_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_leave_valid_handlers_helpers_factories_and_queries_alone() => _result;

    static async Task Verify()
    {
        string[] methods =
        [
            "public static State Reduce(Folded e, State? current) => new();",
            "public static Task<State?> Reduce(Folded e, State? current, EventContext context) => Task.FromResult(current);",
            "public static State Create(Folded e) => new();",
            "public static State Find(string id, State? current) => new();",
            "public static State Helper(Happened e, State? current) => new();",
            "public static State[] All(Folded e, State? current) => [];",
            "public static System.Collections.Generic.IEnumerable<State> All() => [];",
            "public static State Helper(State? current, Folded e) => new();",
            "public static void Apply(Folded e, State? current) { }",
            "public static ValueTask<State> Apply(Folded e, State? current) => new(new State());",
            "public static State Apply(Folded e, State? current, string service) => new();",
            "public static State Apply(Folded e, State? current = null) => new();",
            "public static State Apply(ref Folded e, State? current) => new();",
            "public static State Apply(Folded e, State? current, params EventContext[] context) => new();",
            "public static State Apply<T>(Folded e, State? current) => new();",
            "private static State Apply(Folded e, State? current) => new();"
        ];
        foreach (var method in methods)
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(
                ModelSource("[ReadModel] public record State { " + method + " }"));
        }
    }
}
