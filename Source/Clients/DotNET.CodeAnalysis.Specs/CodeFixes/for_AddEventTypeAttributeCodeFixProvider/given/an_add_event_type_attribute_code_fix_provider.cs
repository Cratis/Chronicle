// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AddEventTypeAttributeCodeFixProvider.given;

public class an_add_event_type_attribute_code_fix_provider : Specification
{
    protected static string CreateEventSequenceSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "",
            "namespace Cratis.Chronicle.Concepts.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.EventSequences",
            "{",
            "    public interface IEventSequence",
            "    {",
            "        void Append(string eventSourceId, object @event);",
            "    }",
            "",
            "    public sealed class EventSequence : IEventSequence",
            "    {",
            "        public void Append(string eventSourceId, object @event)",
            "        {",
            "        }",
            "    }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }

    protected static string CreateReactorSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using System.Threading.Tasks;",
            "",
            "namespace Cratis.Chronicle.Concepts.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    public sealed class EventContext",
            "    {",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Reactors",
            "{",
            "    public interface IReactor",
            "    {",
            "    }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
