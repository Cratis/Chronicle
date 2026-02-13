// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayContexts;

/// <summary>
/// Converter for <see cref="ReplayContext"/> to and from <see cref="ReplayContextEntry"/>.
/// </summary>
public static class ReplayContextEntryConverter
{
    /// <summary>
    /// Convert from <see cref="ReplayContext"/> to <see cref="ReplayContextEntry"/>.
    /// </summary>
    /// <param name="context">The context to convert.</param>
    /// <returns>The <see cref="ReplayContextEntry"/>.</returns>
    public static ReplayContextEntry ToReplayContextEntry(ReplayContext context)
    {
        return new ReplayContextEntry
        {
            ReadModelIdentifier = context.ReadModelIdentifier.Value,
            ReadModel = context.ReadModel.Value,
            RevertModel = context.RevertModel.Value,
            Started = context.Started
        };
    }

    /// <summary>
    /// Convert from <see cref="ReplayContextEntry"/> to <see cref="ReplayContext"/>.
    /// </summary>
    /// <param name="entry">The entry to convert.</param>
    /// <returns>The <see cref="ReplayContext"/>.</returns>
    public static ReplayContext ToReplayContext(ReplayContextEntry entry)
    {
        return new ReplayContext(
            new ReadModelIdentifier(entry.ReadModelIdentifier),
            new ReadModelName(entry.ReadModel),
            new ReadModelName(entry.RevertModel),
            entry.Started);
    }
}
