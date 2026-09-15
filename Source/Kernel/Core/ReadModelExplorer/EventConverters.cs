// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.ReadModelExplorer;

/// <summary>
/// Converts a snapshot's <see cref="Event"/> to its generated contract representation.
/// </summary>
public static class EventConverters
{
    /// <summary>
    /// Converts an <see cref="Event"/> to a contract <see cref="Contracts.ReadModelExplorer.Event"/>.
    /// </summary>
    /// <param name="event">The <see cref="Event"/> to convert.</param>
    /// <returns>The converted contract event.</returns>
    public static Contracts.ReadModelExplorer.Event ToContract(this Event @event) => new()
    {
        Context = @event.Context.ToContract(),
        Content = @event.Content
    };
}
