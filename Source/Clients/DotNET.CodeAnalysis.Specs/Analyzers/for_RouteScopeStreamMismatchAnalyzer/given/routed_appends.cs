// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_RouteScopeStreamMismatchAnalyzer.given;

public class routed_appends : Specification
{
    protected static string Source(string body) => """
        using System;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.EventSequences.Concurrency;
        namespace Cratis.Chronicle.EventSequences.Concurrency
        {
            public class ConcurrencyScope { }
            public class ConcurrencyScopeBuilder
            {
                public ConcurrencyScopeBuilder ForEventSource<T>(string stream = null) => this;
                public ConcurrencyScopeBuilder WithEventStreamType(string stream) => this;
                public ConcurrencyScopeBuilder WithSequenceNumber(ulong number) => this;
                public ConcurrencyScope Build() => new ConcurrencyScope();
            }
        }
        namespace Cratis.Chronicle.EventSequences
        {
            public interface IEventSequence
            {
                void AppendThroughEventSource(Type eventSource, string eventSourceId, object @event, string eventStream = null, ConcurrencyScope concurrencyScope = null);
                void AppendManyThroughEventSource(Type eventSource, string eventSourceId, object[] events, string eventStream = null, ConcurrencyScope concurrencyScope = null);
                void AppendPreparedThroughEventSource(Type eventSource, string eventSourceId, object preparedEvent, string eventStream = null, ConcurrencyScope concurrencyScope = null);
                void AppendPrepared(string eventSourceId, object preparedEvent, string eventStreamType = null, ConcurrencyScope concurrencyScope = null);
                void AppendMany(object[] events, object concurrencyScopes = null);
            }
            public static class EventSequenceEventSourceExtensions
            {
                public static void Append<T>(this IEventSequence sequence, string eventSourceId, object @event, string eventStream = null, ConcurrencyScope concurrencyScope = null) { }
                public static void AppendMany<T>(this IEventSequence sequence, string eventSourceId, object[] events, string eventStream = null, ConcurrencyScope concurrencyScope = null) { }
                public static void AppendPrepared<T>(this IEventSequence sequence, string eventSourceId, object preparedEvent, string eventStream = null, ConcurrencyScope concurrencyScope = null) { }
            }
        }
        class Account { }
        class Other { }
        class Handler
        {
            void Handle(IEventSequence sequence, string unknown)
            {
        """ + Environment.NewLine + body + Environment.NewLine + "    }\n}";
}
