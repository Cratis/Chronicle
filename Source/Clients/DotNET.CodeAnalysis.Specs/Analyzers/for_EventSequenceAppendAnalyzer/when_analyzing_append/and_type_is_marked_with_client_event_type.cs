// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventSequenceAppendAnalyzer.when_analyzing_append;

public class and_type_is_marked_with_client_event_type : Specification
{
    const string Source = """
    using System;
    using Cratis.Chronicle.Events;

    namespace Cratis.Chronicle.Events
    {
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class EventTypeAttribute : Attribute
        {
        }
    }

    namespace Cratis.Chronicle.EventSequences
    {
        public interface IEventSequence
        {
            void Append(string eventSourceId, object @event);
        }

        public sealed class EventSequence : IEventSequence
        {
            public void Append(string eventSourceId, object @event)
            {
            }
        }
    }

    namespace Sample
    {
        [EventType]
        public class KnownEvent
        {
        }

        public class Usage
        {
            public void Append()
            {
                var sequence = new Cratis.Chronicle.EventSequences.EventSequence();
                sequence.Append("source", new KnownEvent());
            }
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventSequenceAppendAnalyzer>.VerifyAnalyzer(Source);

    [Fact] Task should_not_report_diagnostics() => _result;
}
