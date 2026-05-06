// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// Holds well known <see cref="SinkTypeId"/> constants.
/// </summary>
public static class WellKnownSinkTypes
{
    /// <summary>
    /// Gets the identifier of the InMemory projection sink.
    /// </summary>
    public static readonly SinkTypeId InMemory = "8a23995d-da0b-4c4c-818b-f97992f26bbf";

    /// <summary>
    /// Gets the identifier of the Null projection sink.
    /// </summary>
    public static readonly SinkTypeId Null = "00000000-0000-0000-0000-000000000000";

    /// <summary>
    /// Gets the identifier of the MongoDB projection sink.
    /// </summary>
    public static readonly SinkTypeId MongoDB = "22202c41-2be1-4547-9c00-f0b1f797fd75";

    /// <summary>
    /// Gets the identifier of the SQL projection sink.
    /// </summary>
    public static readonly SinkTypeId SQL = "f7d3a1e2-4b5c-4d6e-8f9a-0b1c2d3e4f5a";
}
