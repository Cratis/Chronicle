// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The bookkeeping a <see cref="SinkWriteMode.FencedGlobal"/> write is checked against.
/// </summary>
public partial class Sink
{
    /// <summary>
    /// The bookkeeping a fenced write is checked against, as read with the instance, and the rules for what a fenced write may contain.
    /// </summary>
    /// <param name="Incarnation">The incarnation of the instance read, or null when there was none.</param>
    /// <param name="Revision">The revision of the instance read.</param>
    /// <param name="LastHandled">The last handled event sequence number per writing namespace.</param>
    internal sealed record FencedGlobalState(string? Incarnation, long Revision, IReadOnlyDictionary<string, long> LastHandled)
    {
        /// <summary>
        /// Gets whether the instance did not exist when it was read.
        /// </summary>
        public bool IsAbsent => Incarnation is null;

        /// <summary>
        /// Gets the path of the last handled event sequence number for a writing namespace.
        /// </summary>
        /// <param name="writingNamespace">The namespace name.</param>
        /// <returns>The field path.</returns>
        public static string LastHandledPath(string writingNamespace) =>
            $"{WellKnownProperties.LastHandledByNamespace}.{EncodeNamespace(writingNamespace)}";

        /// <summary>
        /// Encodes a namespace as a field name. Escaping '%' first keeps it injective, so 'a.b' and 'a%2Eb' never share a key.
        /// </summary>
        /// <param name="writingNamespace">The namespace name.</param>
        /// <returns>The encoded field name.</returns>
        public static string EncodeNamespace(string writingNamespace) =>
            writingNamespace.Replace("%", "%25").Replace(".", "%2E").Replace("$", "%24");

        /// <summary>
        /// Reverses <see cref="EncodeNamespace"/>. '%25' is decoded last so a decoded '%' is never read as the start of another escape.
        /// </summary>
        /// <param name="encoded">The encoded field name.</param>
        /// <returns>The namespace name.</returns>
        public static string DecodeNamespace(string encoded) =>
            encoded.Replace("%2E", ".").Replace("%24", "$").Replace("%25", "%");

        /// <summary>
        /// Reads the bookkeeping from the initial state of a changeset.
        /// </summary>
        /// <param name="initialState">The state as read, if any.</param>
        /// <returns>The <see cref="FencedGlobalState"/>.</returns>
        public static FencedGlobalState From(ExpandoObject? initialState)
        {
            var state = initialState as IDictionary<string, object?> ?? new Dictionary<string, object?>();
            if (!HasIncarnation(state, out var incarnation))
            {
                return new(null, 0, new Dictionary<string, long>());
            }

            return new(incarnation, ReadRevision(state), ReadLastHandled(state));
        }

        /// <summary>
        /// Only property changes are fenced; every other kind of change fails closed, so a new kind never writes unprotected.
        /// </summary>
        /// <param name="changeset">The changeset about to be written.</param>
        /// <exception cref="FencedWriteNotSupportedException">Thrown when the changeset holds a change that cannot be fenced.</exception>
        public static void EnsureSupported(IChangeset<AppendedEvent, ExpandoObject> changeset)
        {
            foreach (var change in changeset.Changes.Where(_ => _ is not PropertiesChanged<ExpandoObject>))
            {
                throw new FencedWriteNotSupportedException($"a change of kind '{change.GetType().Name}' is not supported");
            }
        }

        /// <summary>
        /// Gets whether the writing namespace already handled the event.
        /// </summary>
        /// <param name="writingNamespace">The namespace name.</param>
        /// <param name="eventSequenceNumber">The event sequence number.</param>
        /// <returns>True if handled, false otherwise.</returns>
        public bool HasHandled(string writingNamespace, EventSequenceNumber eventSequenceNumber) =>
            LastHandled.TryGetValue(writingNamespace, out var handled) && handled >= (long)eventSequenceNumber.Value;

        static bool HasIncarnation(IDictionary<string, object?> state, out string? incarnation)
        {
            incarnation = state.TryGetValue(WellKnownProperties.Incarnation, out var value) ? value?.ToString() : null;
            return incarnation is not null;
        }

        static long ReadRevision(IDictionary<string, object?> state) =>
            state.TryGetValue(WellKnownProperties.Revision, out var stored) && stored is not null ? Convert.ToInt64(stored) : 0L;

        static Dictionary<string, long> ReadLastHandled(IDictionary<string, object?> state)
        {
            var lastHandled = new Dictionary<string, long>();
            if (!state.TryGetValue(WellKnownProperties.LastHandledByNamespace, out var handled) || handled is not IDictionary<string, object?> byNamespace)
            {
                return lastHandled;
            }

            foreach (var (name, value) in byNamespace.Where(_ => _.Value is not null))
            {
                lastHandled[DecodeNamespace(name)] = Convert.ToInt64(value);
            }

            return lastHandled;
        }
    }
}
