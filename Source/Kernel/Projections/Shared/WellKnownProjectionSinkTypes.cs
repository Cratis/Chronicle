// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Holds well known <see cref="ProjectionSinkTypeId"/> constants.
/// </summary>
public static class WellKnownProjectionSinkTypes
{
    /// <summary>
    /// Gets the identifier of the InMemory projection sink.
    /// </summary>
    public static readonly ProjectionSinkTypeId InMemory = "8a23995d-da0b-4c4c-818b-f97992f26bbf";

    /// <summary>
    /// Gets the identifier of the MongoDB projection sink.
    /// </summary>
    public static readonly ProjectionSinkTypeId MongoDB = "22202c41-2be1-4547-9c00-f0b1f797fd75";

    /// <summary>
    /// Gets the identifier of the outbox event sequence sink.
    /// </summary>
    public static readonly ProjectionSinkTypeId Outbox = "0b36103d-2f76-4a1f-a9f9-b9828aa017c1";
}
