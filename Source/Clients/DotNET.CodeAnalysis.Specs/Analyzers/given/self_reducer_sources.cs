// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.given;

public class self_reducer_sources : Specification
{
    protected static string Source(string declarations) => """
        #nullable enable
        using System;
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Observation;
        using Cratis.Chronicle.Reducers;
        using Cratis.Chronicle.Projections.ModelBound;
        namespace Cratis.Chronicle.Events
        {
            public record EventContext;
            public class EventTypeAttribute : Attribute;
            public class EventStoreAttribute(string name) : Attribute;
        }
        namespace Cratis.Chronicle.EventSequences
        {
            public class EventSequenceAttribute(string name) : Attribute;
            public class EventLogAttribute : Attribute;
        }
        namespace Cratis.Chronicle.Observation { public record Partition(string Value); }
        namespace Cratis.Chronicle.Reducers
        {
            public interface IReducer;
            public interface IReducerFor<TReadModel> : IReducer;
            public class ReducerAttribute(string id = "", string? eventSequence = null, bool isActive = true) : Attribute;
        }
        namespace Cratis.Chronicle.Reactors { public interface IReactor; }
        namespace Cratis.Chronicle.Projections
        {
            public interface IProjectionFor<TReadModel>;
        }
        namespace Cratis.Chronicle.Projections.ModelBound
        {
            public interface IProjectionAnnotation;
            [AttributeUsage(AttributeTargets.All)]
            public class FromEventAttribute<T> : Attribute, IProjectionAnnotation;
        }
        namespace Cratis.Chronicle.ReadModels
        {
            public class PassiveAttribute : Attribute, IProjectionAnnotation;
        }
        namespace Cratis.Chronicle.Registrations
        {
            public class ExplicitArtifacts
            {
                public ExplicitArtifacts RegisterEventType<T>() => this;
                public ExplicitArtifacts RegisterEventType(Type type) => this;
                public ExplicitArtifacts RegisterReducer<T>() => this;
                public ExplicitArtifacts RegisterReducer(Type type) => this;
            }
        }
        namespace Sample
        {
            public record Happened;
        """ + "\n" + declarations + "\n}";
}
