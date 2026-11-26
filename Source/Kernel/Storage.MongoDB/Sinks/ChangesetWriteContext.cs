// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Carries the destination and write seams of one ordered changeset conversion.
/// </summary>
/// <param name="Key">The resolved read-model key.</param>
/// <param name="SequenceNumber">The event position.</param>
/// <param name="Writes">The shared multi-document write seam.</param>
/// <param name="RemoveChild">Removes a child from every matching document at its place in the changeset.</param>
internal record ChangesetWriteContext(Key Key, EventSequenceNumber SequenceNumber, SinkMultiDocumentWrites Writes, Func<ChildRemovedFromAll, Task> RemoveChild);
