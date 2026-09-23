// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_reading_rows_in_a_range.given;

/// <summary>
/// Stores three reminders through the table and orders their grains by the hash the reminder service reads
/// ranges by, so each spec can pick range bounds relative to where the reminders actually sit on the ring.
/// </summary>
public class three_reminders_on_the_ring : for_ReminderTable.given.a_reminder_table
{
    protected GrainId _lowest;
    protected GrainId _middle;
    protected GrainId _highest;
    protected ReminderTableData _result;

    protected uint LowestHash => _lowest.GetUniformHashCode();
    protected uint MiddleHash => _middle.GetUniformHashCode();
    protected uint HighestHash => _highest.GetUniformHashCode();

    async Task Establish()
    {
        var grainIds = new[] { "first", "second", "third" }
            .Select(key => GrainId.Create("observer", key))
            .OrderBy(grainId => grainId.GetUniformHashCode())
            .ToArray();

        _lowest = grainIds[0];
        _middle = grainIds[1];
        _highest = grainIds[2];

        foreach (var grainId in grainIds)
        {
            await _table.UpsertRow(CreateEntry(grainId));
        }
    }

    protected IEnumerable<GrainId> ReadGrainIds => _result.Reminders.Select(_ => _.GrainId);
}
