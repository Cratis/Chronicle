// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Orleans;

/// <summary>
/// Represents an implementation of the reminder table for Orleans.
/// </summary>
public class ReminderTable : IReminderTable
{
    /// <inheritdoc/>
    public Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<ReminderTableData> ReadRows(GrainId grainId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<ReminderTableData> ReadRows(uint begin, uint end) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task TestOnlyClearTable() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<string> UpsertRow(ReminderEntry entry) => throw new NotImplementedException();
}
