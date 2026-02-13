// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Converters between MongoDB EventType and Kernel EventType.
/// </summary>
public static class EventTypeConverters
{
    /// <summary>
    /// Convert to kernel representation.
    /// </summary>
    /// <param name="source">Source MongoDB representation.</param>
    /// <returns>Kernel representation.</returns>
    public static Concepts.Events.EventType ToKernel(this EventType source) =>
        new(source.Id, source.Generation);

    /// <summary>
    /// Convert to MongoDB representation.
    /// </summary>
    /// <param name="source">Source kernel representation.</param>
    /// <returns>MongoDB representation.</returns>
    public static EventType ToMongoDB(this Concepts.Events.EventType source) =>
         new(source.Id, source.Generation);
}
