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

    /// <summary>Prefix of every collection the version 2 unique constraint storage opens ("uc+{name}+uc2" or "uc+{name}+uc2v").</summary>
    const string UniqueConstraintsV2Prefix = "uc+";

    /// <summary>Suffix of version 2 unique constraint collections.</summary>
    const string UniqueConstraintsV2Suffix = "+uc2";

    /// <summary>Suffix of version 2 per-value unique constraint collections.</summary>
    const string UniqueConstraintsV2PerValueSuffix = "+uc2v";

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
            value.StartsWith(WellKnownCollectionNames.CapabilityProbePrefix, StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(ConstraintSuffix, StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(ClosedStreamsSuffix, StringComparison.OrdinalIgnoreCase) ||
            IsUniqueConstraintsV2Collection(value);
    }

    static bool IsUniqueConstraintsV2Collection(string value) =>
        value.StartsWith(UniqueConstraintsV2Prefix, StringComparison.OrdinalIgnoreCase) &&
        (value.EndsWith(UniqueConstraintsV2Suffix, StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(UniqueConstraintsV2PerValueSuffix, StringComparison.OrdinalIgnoreCase));
}
