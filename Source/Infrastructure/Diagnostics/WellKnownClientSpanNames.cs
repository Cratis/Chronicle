// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// The .NET client span names.
/// </summary>
public static class WellKnownClientSpanNames
{
    /// <summary>Appending a single event.</summary>
    public const string EventSequenceAppend = "cratis.chronicle.client.event_sequence.append";

    /// <summary>Appending many events.</summary>
    public const string EventSequenceAppendMany = "cratis.chronicle.client.event_sequence.append_many";

    /// <summary>Committing a unit of work.</summary>
    public const string UnitOfWorkCommit = "cratis.chronicle.client.unit_of_work.commit";

    /// <summary>Rolling back a unit of work.</summary>
    public const string UnitOfWorkRollback = "cratis.chronicle.client.unit_of_work.rollback";

    /// <summary>Handling an event in a reactor.</summary>
    public const string ReactorHandle = "cratis.chronicle.client.reactor.handle";

    /// <summary>Handling an event in a reducer.</summary>
    public const string ReducerHandle = "cratis.chronicle.client.reducer.handle";
}
