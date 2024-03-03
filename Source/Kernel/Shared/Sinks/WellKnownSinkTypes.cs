// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Sinks;

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
}
