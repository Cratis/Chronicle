// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.given;

public class a_non_awaited_assertion_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        // Mirrors the real assertion surface: the event-sequence assertions return an awaitable while the
        // sibling result assertions are synchronous and void — the mix that makes the trap invisible at the
        // call site. The kernel-backed integration assertions live in their own Cratis testing namespace, and
        // the unrelated third-party surface is the noise the namespace gate exists to keep out.
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
            "        public static Task<int> ShouldHaveAppendedEvents(this IEventSequence eventSequence) => Task.FromResult(0);",
            "        public static ValueTask ShouldHaveNoEvents(this IEventSequence eventSequence) => default;",
            "        public static ValueTask<int> ShouldHaveNextSequenceNumber(this IEventSequence eventSequence) => default;",
            "        public static void ShouldBeSuccessful(this IEventSequence eventSequence) { }",
            "    }",
            "",
            "    public class EventSequenceAssertions",
            "    {",
            "        public Task ShouldHaveBeenAppendedTo() => Task.CompletedTask;",
            "        public static Task ShouldHaveBeenStarted(IEventSequence eventSequence) => Task.CompletedTask;",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.XUnit.Integration.Events",
            "{",
            "    public interface IChronicleSetupFixture { }",
            "    public static class EventsShouldExtensions",
            "    {",
            "        public static Task ShouldHaveTailSequenceNumber(this IChronicleSetupFixture fixture, int expected) => Task.CompletedTask;",
            "    }",
            "}",
            "",
            "namespace Contoso.Ordering",
            "{",
            "    public interface IOrder { }",
            "    public static class OrderShouldExtensions",
            "    {",
            "        public static Task ShouldHaveBeenPlaced(this IOrder order) => Task.CompletedTask;",
            "    }",
            "}",
            "",
            "namespace Sample",
            "{",
            "    using Contoso.Ordering;",
            "    using Cratis.Chronicle.Testing.EventSequences;",
            "    using Cratis.Chronicle.XUnit.Integration.Events;",
            "",
            usage,
            "}"
        ]);
    }
}
