// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Removes the persistent reminders of observers that belong to the event store <see cref="EventStoreName.NotSet"/>.
/// </summary>
/// <remarks>
/// A client call without an event store once created observers for <see cref="EventStoreName.NotSet"/>, and those observers left reminders
/// behind. Such an observer can never activate - its storage refuses the name - so its reminder ticks, fails and logs a
/// stack trace every minute, for as long as the reminder exists. Nothing inside the grain can remove it.
/// </remarks>
/// <param name="reminderTable">The <see cref="IReminderTable"/> holding the persistent reminders of the observers.</param>
/// <param name="logger">The logger.</param>
public class RemoveNotSetObserverReminders(IReminderTable reminderTable, ILogger<RemoveNotSetObserverReminders> logger) : ICanApplyPatch
{
    const string ObserverGrainType = "observer";

    /// <inheritdoc/>
    public SemanticVersion Version => new(19, 118, 0);

    /// <inheritdoc/>
    public async Task Up()
    {
        // The table is read in two ranges because the first does not include a reminder hashed to zero.
        var rows = (await reminderTable.ReadRows(0, uint.MaxValue)).Reminders
            .Concat((await reminderTable.ReadRows(uint.MaxValue, 0)).Reminders)
            .Where(IsNotSetObserverReminder)
            .ToArray();

        foreach (var row in rows)
        {
            if (await reminderTable.RemoveRow(row.GrainId, row.ReminderName, row.ETag!))
            {
                logger.RemovedReminder(row.ReminderName, row.GrainId.ToString());
            }
        }

        logger.RemovedReminders(rows.Length);
    }

    static bool IsNotSetObserverReminder(ReminderEntry row) =>
        row.GrainId.Type.ToString() == ObserverGrainType
        && NamespaceRemover.TryParseKey(row.GrainId.Key.ToString(), out var key)
        && key.EventStore == EventStoreName.NotSet;
}
