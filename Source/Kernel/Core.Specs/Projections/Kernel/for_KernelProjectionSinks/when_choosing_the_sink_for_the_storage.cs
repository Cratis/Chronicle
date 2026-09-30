// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionSinks;

public class when_choosing_the_sink_for_the_storage : Specification
{
    [Fact] void should_use_mongodb_for_mongodb() => KernelProjectionSinks.ForStorage("MongoDB").ShouldEqual(WellKnownSinkTypes.MongoDB);
    [Fact] void should_use_sql_for_sqlite() => KernelProjectionSinks.ForStorage("Sqlite").ShouldEqual(WellKnownSinkTypes.SQL);
    [Fact] void should_use_sql_for_sql_server() => KernelProjectionSinks.ForStorage("mssql").ShouldEqual(WellKnownSinkTypes.SQL);
    [Fact] void should_use_sql_for_postgresql() => KernelProjectionSinks.ForStorage("postgresql").ShouldEqual(WellKnownSinkTypes.SQL);
    [Fact] void should_use_in_memory_for_in_memory() => KernelProjectionSinks.ForStorage("InMemory").ShouldEqual(WellKnownSinkTypes.InMemory);
    [Fact] void should_default_to_mongodb() => KernelProjectionSinks.ForStorage(string.Empty).ShouldEqual(WellKnownSinkTypes.MongoDB);
}
