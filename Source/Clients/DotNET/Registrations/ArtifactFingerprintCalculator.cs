// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Computes a deterministic fingerprint from prepared wire definitions, without changing the payloads sent.
/// The format is versioned by its tag; any encoding change must bump the tag's :1 version.
/// </summary>
internal static class ArtifactFingerprintCalculator
{
    /// <summary>
    /// Computes the fingerprint, excluding transient registration-envelope metadata.
    /// </summary>
    /// <param name="definitions">The prepared definitions.</param>
    /// <returns>The lowercase SHA-256 fingerprint.</returns>
    public static ArtifactFingerprint Calculate(ArtifactDefinitionPayloads definitions)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Frame(hash, Encoding.UTF8.GetBytes("cratis-chronicle-artifact-fingerprint:1"));
        Write(hash, "event-types", definitions.EventTypes.Select(CloneEventType).OrderBy(_ => _.Type.Id, StringComparer.Ordinal).ThenBy(_ => _.Type.Generation));
        Write(hash, "event-sources", definitions.EventSources.Select(_ => Serializer.DeepClone(_)).OrderBy(_ => _.Name, StringComparer.Ordinal));
        Write(hash, "read-models", definitions.ReadModels.Select(_ => Serializer.DeepClone(_)).OrderBy(_ => _.Type.Identifier, StringComparer.Ordinal).ThenBy(_ => _.Type.Generation));
        Write(hash, "constraints", definitions.Constraints.Select(_ => Serializer.DeepClone(_)).OrderBy(_ => _.Name, StringComparer.Ordinal));
        Write(hash, "projections", definitions.Projections.Select(CloneProjection).OrderBy(_ => _.Identifier, StringComparer.Ordinal));
        return new ArtifactFingerprint(Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    static Contracts.Events.EventTypeRegistration CloneEventType(Contracts.Events.EventTypeRegistration definition)
    {
        var clone = Serializer.DeepClone(definition);
        clone.EventStore = string.Empty;
        return clone;
    }

    static ProjectionDefinition CloneProjection(ProjectionDefinition definition)
    {
        var clone = Serializer.DeepClone(definition);
        clone.LastUpdated = null;
        clone.From = Sort(clone.From);
        foreach (var from in clone.From.Values)
        {
            from.Properties = Sort(from.Properties);
        }

        clone.Join = Sort(clone.Join);
        foreach (var join in clone.Join.Values)
        {
            join.Properties = Sort(join.Properties);
        }

        clone.Children = Sort(clone.Children);
        clone.Nested = Sort(clone.Nested);
        foreach (var child in clone.Children.Values.Concat(clone.Nested.Values))
        {
            Normalize(child);
        }

        clone.All.Properties = Sort(clone.All.Properties);
        foreach (var from in clone.FromEvery)
        {
            from.From.Properties = Sort(from.From.Properties);
        }

        clone.RemovedWith = Sort(clone.RemovedWith);
        clone.RemovedWithJoin = Sort(clone.RemovedWithJoin);
        return clone;
    }

    static void Normalize(ChildrenDefinition definition)
    {
        definition.From = Sort(definition.From);
        foreach (var from in definition.From.Values)
        {
            from.Properties = Sort(from.Properties);
        }

        definition.Join = Sort(definition.Join);
        foreach (var join in definition.Join.Values)
        {
            join.Properties = Sort(join.Properties);
        }

        definition.All.Properties = Sort(definition.All.Properties);
        definition.RemovedWith = Sort(definition.RemovedWith);
        definition.RemovedWithJoin = Sort(definition.RemovedWithJoin);
        definition.Children = Sort(definition.Children);
        definition.Nested = Sort(definition.Nested);
        foreach (var child in definition.Children.Values.Concat(definition.Nested.Values))
        {
            Normalize(child);
        }
    }

    static Dictionary<string, TValue> Sort<TValue>(IDictionary<string, TValue> dictionary) =>
        dictionary.OrderBy(_ => _.Key, StringComparer.Ordinal).ToDictionary(_ => _.Key, _ => _.Value, StringComparer.Ordinal);

    static Dictionary<EventType, TValue> Sort<TValue>(IDictionary<EventType, TValue> dictionary) =>
        dictionary.OrderBy(_ => _.Key.Id, StringComparer.Ordinal).ThenBy(_ => _.Key.Generation).ThenBy(_ => _.Key.Tombstone).ToDictionary(_ => _.Key, _ => _.Value);

    static void Write<T>(IncrementalHash hash, string section, IEnumerable<T> definitions)
    {
        var items = definitions.ToArray();
        Frame(hash, Encoding.UTF8.GetBytes(section));
        Count(hash, items.Length);
        foreach (var definition in items)
        {
            using var item = new MemoryStream();
            Serializer.Serialize(item, definition);
            Frame(hash, item.ToArray());
        }
    }

    static void Frame(IncrementalHash hash, byte[] bytes)
    {
        Count(hash, bytes.Length);
        hash.AppendData(bytes);
    }

    static void Count(IncrementalHash hash, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        hash.AppendData(bytes);
    }
}
