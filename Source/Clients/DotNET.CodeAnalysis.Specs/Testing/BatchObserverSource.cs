// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing;

/// <summary>
/// Builds analyzer sources around stand-ins for the client types batch methods use.
/// </summary>
public static class BatchObserverSource
{
    /// <summary>
    /// Creates a source that holds the client stand-ins, three event types and the given usage in namespace Sample.
    /// </summary>
    /// <param name="usage">The code under analysis.</param>
    /// <returns>The source.</returns>
    public static string Create(string usage) => $$"""
        #nullable enable
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading;
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Observation;
        using Cratis.Chronicle.Reactors;
        using Cratis.Chronicle.Reducers;

        namespace Cratis.Chronicle.Events
        {
            [AttributeUsage(AttributeTargets.Class)]
            public sealed class EventTypeAttribute : Attribute {}
            public sealed class EventContext {}
            public sealed record EventAndContext(object Event, EventContext Context);
        }

        namespace Cratis.Chronicle.Observation
        {
            public sealed class EventBatch : IReadOnlyList<EventAndContext>
            {
                public int Count => 0;
                public EventAndContext this[int index] => throw new NotImplementedException();
                public IEnumerator<EventAndContext> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public record BatchContext(CancellationToken CancellationToken);

            [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
            public abstract class ObservesAttribute : Attribute {}

            [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
            public sealed class ObservesAttribute<TEvent> : ObservesAttribute where TEvent : class {}
        }

        namespace Cratis.Chronicle.Reactors
        {
            public interface IReactor {}
        }

        namespace Cratis.Chronicle.Reducers
        {
            public interface IReducer {}
            public interface IReducerFor<TReadModel> : IReducer {}
        }

        namespace Sample
        {
            [EventType] public class UserRegistered {}
            [EventType] public class UserRenamed {}
            [EventType] public class UserRemoved {}
            public interface ISearchIndex {}

        {{usage}}
        }
        """;
}
