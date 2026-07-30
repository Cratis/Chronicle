// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.given;

public class a_non_awaited_assertion_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        // Mirrors the real assertion surface: the event-sequence assertions return Task while the sibling
        // result assertions are synchronous and void — the mix that makes the trap invisible at the call site.
        return string.Join(Environment.NewLine,
        [
            "using System.Threading.Tasks;",
            "",
            "namespace Cratis.Chronicle.Testing.EventSequences",
            "{",
            "    public interface IEventSequence { }",
            "    public static class EventSequenceShouldExtensions",
            "    {",
            "        public static Task ShouldHaveAppendedEvent<TEvent>(this IEventSequence eventSequence) => Task.CompletedTask;",
            "        public static Task ShouldHaveTailSequenceNumber(this IEventSequence eventSequence, int expected) => Task.CompletedTask;",
            "        public static void ShouldBeSuccessful(this IEventSequence eventSequence) { }",
            "    }",
            "}",
            "",
            "namespace Sample",
            "{",
            "    using Cratis.Chronicle.Testing.EventSequences;",
            "",
            usage,
            "}"
        ]);
    }
}
