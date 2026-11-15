// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents the changes of one instance in a page written through <see cref="ISinkPages"/>.
/// </summary>
/// <param name="Key">The <see cref="Key"/> of the instance.</param>
/// <param name="Changeset">The changes to apply.</param>
/// <param name="EventSequenceNumber">The sequence number of the event that caused the changes.</param>
/// <param name="Mode">The <see cref="SinkWriteMode"/> to apply the changes with.</param>
public record SinkPageWrite(Key Key, IChangeset<AppendedEvent, ExpandoObject> Changeset, EventSequenceNumber EventSequenceNumber, SinkWriteMode Mode);
