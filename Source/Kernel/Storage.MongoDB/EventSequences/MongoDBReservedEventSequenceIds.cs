// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Derives reserved event sequence names from the MongoDB storage catalog.
/// </summary>
public class MongoDBReservedEventSequenceIds : IReservedEventSequenceIds
{
    /// <summary>Derived per event sequence by UniqueConstraintsStorage ("{eventSequenceId}+{constraint}+constraint").</summary>
    const string ConstraintSuffix = "+constraint";

    /// <summary>Derived per event sequence by ClosedStreamsConstraintStorage ("{eventSequenceId}+closed_streams").</summary>
    const string ClosedStreamsSuffix = "+closed_streams";

    static readonly HashSet<string> _names = new(
        typeof(WellKnownCollectionNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(_ => _.IsLiteral && _.FieldType == typeof(string))
            .Select(_ => (string)_.GetRawConstantValue()!),
        StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool IsReserved(EventSequenceId eventSequenceId)
    {
        var value = eventSequenceId.Value;
        return _names.Contains(value) ||
            value.EndsWith(ConstraintSuffix, StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(ClosedStreamsSuffix, StringComparison.OrdinalIgnoreCase);
    }
}
