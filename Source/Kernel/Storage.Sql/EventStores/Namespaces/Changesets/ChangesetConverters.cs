// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Changesets;

/// <summary>
/// Converter methods for working with <see cref="IChangeset{TSource, TTarget}"/> converting to and from SQL representations.
/// </summary>
public static class ChangesetConverters
{
    /// <summary>
    /// Convert to a <see cref="Changeset">SQL</see> representation.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelContainerName"/>.</param>
    /// <param name="readModelKey">The <see cref="Key"/>.</param>
    /// <param name="eventType">The <see cref="EventType"/>.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/>.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/>.</param>
    /// <param name="changeset">The <see cref="IChangeset{TSource, TTarget}"/> to convert.</param>
    /// <returns>Converted <see cref="Changeset"/>.</returns>
    public static Changeset ToSql(
        ReadModelContainerName readModel,
        Key readModelKey,
        EventType eventType,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            ReadModelName = readModel.Value,
            ReadModelKey = readModelKey.ToString(),
            EventType = eventType.Id.Value,
            SequenceNumber = sequenceNumber.Value,
            CorrelationId = correlationId.Value,
            ChangesetData = JsonSerializer.Serialize(changeset.Changes)
        };
}
