// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>Supplies the owning sink's direct-write boundary without changing the public converter contract.</summary>
internal interface IChangesetConverterWithDirectWrites
{
    /// <summary>
    /// Yields each direct update before executing the joins and removals that follow it.
    /// </summary>
    /// <param name="changeset">The ordered changes to convert.</param>
    /// <param name="context">The destination and shared write seams.</param>
    /// <returns>The consecutive direct updates, interleaved with awaited multi-document writes.</returns>
    IAsyncEnumerable<UpdateDefinitionAndArrayFilters> ToUpdatesInOrder(IChangeset<AppendedEvent, ExpandoObject> changeset, ChangesetWriteContext context);

    /// <summary>Converts a changeset, routing any direct multi-document mutations through the supplied boundary.</summary>
    /// <param name="key">The changeset key.</param>
    /// <param name="changeset">The changes to convert.</param>
    /// <param name="eventSequenceNumber">The event watermark.</param>
    /// <param name="directWrites">The owning sink's direct-write boundary.</param>
    /// <returns>The converted keyed update.</returns>
    Task<UpdateDefinitionAndArrayFilters> ToUpdateDefinition(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkMultiDocumentWrites directWrites);
}
