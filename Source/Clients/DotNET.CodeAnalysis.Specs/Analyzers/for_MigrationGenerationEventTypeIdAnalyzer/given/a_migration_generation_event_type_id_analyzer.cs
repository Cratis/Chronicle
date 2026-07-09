// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MigrationGenerationEventTypeIdAnalyzer.given;

public class a_migration_generation_event_type_id_analyzer : Specification
{
    protected static string CreateSource(string usage)
    {
        return string.Join(Environment.NewLine,
        [
            "using System;",
            "using Cratis.Chronicle.Events;",
            "using Cratis.Chronicle.Events.Migrations;",
            "",
            "namespace System.Runtime.CompilerServices",
            "{",
            "    public sealed class IsExternalInit { }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events",
            "{",
            "    [AttributeUsage(AttributeTargets.Class)]",
            "    public sealed class EventTypeAttribute : Attribute",
            "    {",
            "        public EventTypeAttribute(string id = \"\", uint generation = 1) { }",
            "    }",
            "}",
            "",
            "namespace Cratis.Chronicle.Events.Migrations",
            "{",
            "    public abstract class EventTypeMigration<TUpgrade, TPrevious> { }",
            "}",
            "",
            "namespace Sample",
            "{",
            usage,
            "}"
        ]);
    }
}
