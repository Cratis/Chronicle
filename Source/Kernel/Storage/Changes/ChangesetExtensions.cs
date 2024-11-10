// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Changes;

/// <summary>
/// Extension methods for <see cref="IChangeset{TSource, TTarget}"/>.
/// </summary>
public static class ChangesetExtensions
{
    const string SequenceNumberProperty = "__lastHandledEventSequenceNumber";

    /// <summary>
    /// Add a change to set the sequence number on the state.
    /// </summary>
    /// <param name="changeset"><see cref="IChangeset{TIncoming, TTarget}"/> to work with.</param>
    public static void SetSequenceNumber(this IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var state = changeset.CurrentState as IDictionary<string, object>;
        state.TryGetValue(SequenceNumberProperty, out var existing);
        state[SequenceNumberProperty] = changeset.Incoming.Metadata.SequenceNumber;

        var difference = new PropertyDifference(SequenceNumberProperty, existing, changeset.Incoming.Metadata.SequenceNumber.Value);
        changeset.Add(new PropertiesChanged<ExpandoObject>(state, [difference]));
    }
}
