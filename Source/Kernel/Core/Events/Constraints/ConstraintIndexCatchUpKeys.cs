// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Retains bounded fingerprints of keys touched on the live side of a catch-up boundary.
/// </summary>
internal sealed class ConstraintIndexCatchUpKeys
{
    readonly HashSet<Fingerprint> _keys = [];

    /// <summary>
    /// Gets the number of retained fingerprints.
    /// </summary>
    internal int Count => _keys.Count;

    /// <summary>
    /// Discards exclusions after a persisted boundary raise.
    /// </summary>
    internal void Clear() => _keys.Clear();

    /// <summary>
    /// Determines whether the key or its whole source was touched live.
    /// </summary>
    /// <param name="name">The constraint.</param>
    /// <param name="scope">The scope key.</param>
    /// <param name="source">The event source.</param>
    /// <param name="value">The per-value key, absent in per-source mode.</param>
    /// <returns>Whether catch-up must skip the key.</returns>
    internal bool Contains(ConstraintName name, string scope, EventSourceId source, UniqueConstraintValue? value) =>
        _keys.Contains(Hash(name, scope, source, null)) || _keys.Contains(Hash(name, scope, source, value));

    /// <summary>
    /// Excludes the key affected by a live claim or removal, regardless of update success.
    /// </summary>
    /// <param name="definition">The unique definition.</param>
    /// <param name="context">The live plaintext inputs.</param>
    internal void Include(UniqueConstraintDefinition definition, ConstraintValidationContext context)
    {
        var scope = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);
        if (definition.RemovedWith.Contains(context.EventTypeId))
        {
            var removal = definition.RemovalEventDefinitions.SingleOrDefault(item => item.EventTypeId == context.EventTypeId);
            if (definition.Mode == UniqueConstraintMode.PerValue && removal?.Properties.Any() == true)
            {
                var properties = removal.GetPropertiesAndValues(context).ToArray();
                if (properties.Length > 0) Add(definition, scope, context.EventSourceId, new(properties.GetValue(definition.IgnoreCasing)));
            }
            else
            {
                Add(definition, scope, context.EventSourceId, null);
            }
        }
        else if (definition.SupportsEventType(context.EventTypeId))
        {
            var properties = definition.GetPropertiesAndValues(context).ToArray();
            if (definition.Mode == UniqueConstraintMode.PerEventSource || properties.Length > 0)
            {
                Add(definition, scope, context.EventSourceId, properties.Length == 0 ? null : new(properties.GetValue(definition.IgnoreCasing)));
            }
        }
    }

    static Fingerprint Hash(ConstraintName name, string scope, EventSourceId source, UniqueConstraintValue? value)
    {
        // Length-prefix each component: domain strings may themselves contain separators.
        var components = new[] { name.Value, scope, source.Value, value?.Value };
        var input = string.Concat(components.Select(component => component is null ? "-1:" : $"{component.Length}:{component}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new(BitConverter.ToUInt64(hash, 0), BitConverter.ToUInt64(hash, 8), BitConverter.ToUInt64(hash, 16), BitConverter.ToUInt64(hash, 24));
    }

    void Add(UniqueConstraintDefinition definition, string scope, EventSourceId source, UniqueConstraintValue? value) =>
        _keys.Add(Hash(definition.Name, scope, source, definition.Mode == UniqueConstraintMode.PerValue ? value : null));

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Fingerprint(ulong First, ulong Second, ulong Third, ulong Fourth);
}
