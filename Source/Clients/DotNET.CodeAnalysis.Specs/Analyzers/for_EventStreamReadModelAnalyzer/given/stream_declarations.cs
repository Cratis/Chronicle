// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventStreamReadModelAnalyzer.given;

public class stream_declarations
{
    protected static string Source(string declaration) => """
        using System;
        using System.Linq.Expressions;
        using Cratis.Chronicle.EventSources;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Chronicle.Projections;
        [EventSource("BankAccount")]
        [Cratis.Chronicle.EventSources.EventStream("Transactions")]
        public class BankAccount : IEventSource {}
        """ + declaration + """
        namespace Cratis.Chronicle.EventSources
        {
            public interface IEventSource {}
            public class EventSourceAttribute(string name) : Attribute {}
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            public class EventStreamAttribute(string name) : Attribute {}
        }
        namespace Cratis.Chronicle.ReadModels
        {
            public class EventStreamAttribute<TSource>(string stream) : Attribute where TSource : IEventSource {}
        }
        namespace Cratis.Chronicle.Reducers
        {
            public interface IReducerFor<T> {}
        }
        namespace Cratis.Chronicle.Events
        {
            public class EventContext { public string EventStreamId => "month"; public string EventSourceId => "account"; }
        }
        namespace Cratis.Chronicle.Projections
        {
            public class KeyBuilder<TReadModel, TEvent, TBuilder>
            {
                public void UsingKeyFromContext(Expression<Func<Cratis.Chronicle.Events.EventContext, object>> key) {}
            }
        }
        """;
}
