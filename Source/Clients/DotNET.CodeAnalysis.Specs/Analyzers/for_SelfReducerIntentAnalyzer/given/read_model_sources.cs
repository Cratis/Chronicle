// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.given;

public class read_model_sources : Analyzers.given.self_reducer_sources
{
    protected static string ModelSource(string declarations) =>
        "using Cratis.Arc.Queries.ModelBound;\n" + Source("[EventType] public record Folded;\n" + declarations) + """

        namespace Cratis.Chronicle.Events
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
            public abstract class EventTypeGenerationForAttribute(uint generation) : System.Attribute;
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
            public sealed class EventTypeGenerationForAttribute<TEventType>(uint generation) : EventTypeGenerationForAttribute(generation)
                where TEventType : class;
        }
        namespace Cratis.Arc.Queries.ModelBound
        {
            public class ReadModelAttribute : System.Attribute;
        }
        """;
}
