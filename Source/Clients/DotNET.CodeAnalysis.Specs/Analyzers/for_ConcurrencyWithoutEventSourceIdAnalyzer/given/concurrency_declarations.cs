// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConcurrencyWithoutEventSourceIdAnalyzer.given;

public class concurrency_declarations : Specification
{
    protected static string Source(string usage) => """
        using System;
        using Cratis.Chronicle.EventSources;
        using Dimensions = Cratis.Chronicle.EventSources.ConcurrencyDimensions;
        namespace Cratis.Chronicle.EventSources
        {
            [Flags] public enum ConcurrencyDimensions { None = 0, EventSourceId = 1, EventSourceType = 2, EventStreamType = 4, EventStreamId = 8 }
            public interface IEventSource { }
            public sealed class EventSourceAttribute : Attribute { public ConcurrencyDimensions Concurrency { get; set; } }
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            public sealed class EventStreamAttribute : Attribute
            {
                public EventStreamAttribute(string name) { }
                public ConcurrencyDimensions Concurrency { get; set; }
            }
        }
        """ + Environment.NewLine + usage;
}
