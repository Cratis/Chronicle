// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.SQL.Sinks;

/// <summary>
/// Defines a converter for converting changesets to SQL operations.
/// </summary>
public interface IChangesetConverter
{
    /// <summary>
    /// Convert a changeset to SQL operations.
    /// </summary>
    /// <param name="key">The key for the model.</param>
    /// <param name="changeset">The changeset to convert.</param>
    /// <param name="eventSequenceNumber">The event sequence number.</param>
    /// <returns>SQL operations to execute.</returns>
    Task<SqlOperations> ConvertToSqlOperations(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber);
}