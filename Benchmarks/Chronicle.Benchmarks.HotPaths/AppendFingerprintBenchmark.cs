// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Idempotency;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Monads;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Compares append fingerprinting with the unchanged implementation from 81fb14c.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("AppendFingerprint")]
public class AppendFingerprintBenchmark
{
    AppendFingerprintInput _input = null!;
    ProtectedValueUse[] _uses = null!;

    /// <summary>Gets or sets the number of protected boundaries.</summary>
    [Params(0, 1, 32)]
    public int ProtectedCount { get; set; }

    /// <summary>Gets or sets the unprotected payload size.</summary>
    [Params(128, 16384)]
    public int PayloadSize { get; set; }

    /// <summary>Prepares detached plaintext and stable protection reports outside measurement.</summary>
    /// <exception cref="InvalidOperationException">The baseline and production fingerprints differ.</exception>
    [GlobalSetup]
    public void Setup()
    {
        var content = new JsonObject { ["payload"] = new string('x', PayloadSize) };
        var key = new EncryptionKey([1, 2, 3], [4, 5, 6]);
        var locations = new List<ProtectedValueLocation>();
        var uses = new List<ProtectedValueUse>();
        for (var index = 0; index < ProtectedCount; index++)
        {
            var path = $"value{index}";
            content[path] = $"protected-{index}-é😀";
            var plaintext = content[path]!.DeepClone();
            locations.Add(new(path, ProtectionKind.Pii, "subject", plaintext));
            uses.Add(new(path, ProtectionKind.Pii, plaintext.DeepClone(), new(ProtectionKind.Pii, "subject", key, new EncryptionKeyRevision(3))));
        }

        _input = new(new("event", 1), content, "source", EventSourceType.Default, EventStreamType.All, EventStreamId.Default, null, "source", "append-key:test", locations);
        _uses = [.. uses];
        if (ComputeBaseline() != ComputeCurrent())
        {
            throw new InvalidOperationException("Benchmark implementations disagree.");
        }
    }

    /// <summary>Fingerprints with the unchanged baseline.</summary>
    /// <returns>The persisted representation, consuming every digest.</returns>
    [Benchmark(Baseline = true)]
    public string ComputeBaseline() => Format(BaselineFingerprint.Compute(_input, _uses));

    /// <summary>Fingerprints with the production implementation.</summary>
    /// <returns>The persisted representation, consuming every digest.</returns>
    [Benchmark]
    public string ComputeCurrent() => Format(AppendFingerprint.Compute(_input, _uses));

    static string Format(Result<StoredAppendFingerprint, FingerprintFailure> result) => result.TryGetResult(out var fingerprint)
        ? fingerprint.Format()
        : throw new InvalidOperationException("Fingerprint refused benchmark input.");

    /// <summary>Preserves the algorithm from 81fb14c independently of production candidate edits.</summary>
    static class BaselineFingerprint
    {
        internal static Result<StoredAppendFingerprint, FingerprintFailure> Compute(AppendFingerprintInput input, IReadOnlyList<ProtectedValueUse> uses)
        {
            var plain = Plain(input);
            if (!plain.TryGetResult(out var hash) || !ReportsMatch(input.ProtectedValues, uses))
            {
                return FingerprintFailure.FingerprintUnsupported;
            }

            var parts = new List<ProtectedFingerprintPart>();
            foreach (var group in uses.GroupBy(use => use.Kind).OrderBy(group => group.Key))
            {
                var key = group.First().Key;
                if (group.Any(use => use.Key.KeyReference != key.KeyReference || use.Key.Revision != key.Revision))
                {
                    return FingerprintFailure.FingerprintKeysDiverged;
                }

                if (group.Any(use => use.Key.Identifier != key.Identifier))
                {
                    return FingerprintFailure.FingerprintUnsupported;
                }

                var values = group.Select(use => new ProtectedValueLocation(use.Path, use.Kind, use.Key.Identifier, use.Plaintext));
                parts.Add(new(group.Key, key.KeyReference, key.Revision, ProtectedHash(input.Identity, key.Key, values)));
            }

            return new StoredAppendFingerprint(hash, input.EventType, parts.ToArray());
        }

        static Result<string, FingerprintFailure> Plain(AppendFingerprintInput input)
        {
            var masked = FingerprintContent.Mask(input);
            if (masked is null)
            {
                return FingerprintFailure.FingerprintUnsupported;
            }

            string[] fields = [input.EventType.Id.Value, input.EventType.Generation.Value.ToString(CultureInfo.InvariantCulture),
                CanonicalJson.Encode(masked), input.EventSourceId.Value, input.EventSourceType.Value, input.EventStreamType.Value,
                input.EventStreamId.Value, input.EventSource?.Value ?? string.Empty, input.Subject];
            var boundaries = input.ProtectedValues.OrderBy(value => value.Path, StringComparer.Ordinal)
                .SelectMany(value => new[] { value.Path, FingerprintEncoding.Name(value.Kind)! });
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(FingerprintEncoding.Fields(fields.Concat(boundaries)))));
        }

        static string ProtectedHash(string identity, EncryptionKey key, IEnumerable<ProtectedValueLocation> values)
        {
            var derived = HKDF.DeriveKey(HashAlgorithmName.SHA256, key.Private, 32, Encoding.UTF8.GetBytes(identity), "cratis-chronicle/append-fingerprint/v1"u8.ToArray());
            try
            {
                var fields = values.OrderBy(value => value.Path, StringComparer.Ordinal).SelectMany(value => new[] { value.Path, CanonicalJson.Encode(value.Plaintext) });
                return Convert.ToHexStringLower(HMACSHA256.HashData(derived, Encoding.UTF8.GetBytes(FingerprintEncoding.Fields(fields))));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(derived);
            }
        }

        static bool ReportsMatch(IReadOnlyList<ProtectedValueLocation> locations, IReadOnlyList<ProtectedValueUse> uses) =>
            locations.Count == uses.Count && uses.Select(use => use.Path).Distinct(StringComparer.Ordinal).Count() == uses.Count &&
            locations.All(location => uses.Any(use => use.Path == location.Path && use.Kind == location.Kind && use.Key.Kind == location.Kind &&
                use.Key.Identifier == location.Identifier && use.Key.Revision.Value > 0 && use.Key.Revision.Value < uint.MaxValue &&
                CanonicalJson.Encode(use.Plaintext) == CanonicalJson.Encode(location.Plaintext)));
    }
}
