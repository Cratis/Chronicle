// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Provides the compiled state read for an event sequence, scoped to the EF model it executes against.
/// </summary>
/// <remarks>
/// A compiled query binds to the model of the first context that runs it, and different providers can
/// produce different models within one process. Delegates are therefore created once per model and are
/// released together with the model.
/// </remarks>
static class EventSequenceStateQueries
{
    static readonly ConditionalWeakTable<Microsoft.EntityFrameworkCore.Metadata.IModel, Func<NamespaceDbContext, string, Task<EventSequenceState?>>> _perModel = [];

    /// <summary>
    /// Reads the state entity for an event sequence without change tracking.
    /// </summary>
    /// <param name="context">The <see cref="NamespaceDbContext"/> to read with.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>The entity, or null when none has been stored.</returns>
    public static Task<EventSequenceState?> Get(NamespaceDbContext context, string eventSequenceId) =>
        _perModel.GetValue(context.Model, static _ => EF.CompileAsyncQuery(
            static (NamespaceDbContext db, string id) => db.EventSequences.AsNoTracking().FirstOrDefault(s => s.EventSequenceId == id)))(context, eventSequenceId);
}
