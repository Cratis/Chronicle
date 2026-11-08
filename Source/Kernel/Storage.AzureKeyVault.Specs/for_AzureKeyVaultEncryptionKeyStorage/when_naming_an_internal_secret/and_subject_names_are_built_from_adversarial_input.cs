// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_naming_an_internal_secret;

/// <summary>
/// Builds every subject key name, erasure name and subject prefix over a set of crafted event stores, namespaces and
/// identifiers, and shows that none of them equals, or would enumerate, an internal secret name.
/// </summary>
public class and_subject_names_are_built_from_adversarial_input : Specification
{
    static readonly string[] _crafted =
    [
        "event-store",
        "internal",
        "INTERNAL",
        "-internal-",
        "chronicle-internal",
        "chronicle-internal--event-store",
        "chronicle-k1",
        "chronicle-k1--",
        "--internal",
        "internal--event-store",
        "internal--event-store--mutation-receipt-digest-v2",
        "mutation-receipt-digest-v2",
        "chronicle-mutation-receipt-digest-v2",
        "$chronicle-mutation-receipt-digest$v2$",
        "$CHRONICLE-Mutation-Receipt-Digest$V2$",
        "i",
        "-",
        "--",
        "!!internal!!",
        " internal ",
        "internal/event_store.mutation receipt digest v2",
        "\u0130nternal",
        ""
    ];

    static readonly EncryptionKeyRevision[] _revisions = [EncryptionKeyRevision.Initial, new(2u), new(uint.MaxValue)];

    List<string> _internalNames;
    List<string> _subjectNames;
    List<string> _subjectPrefixes;

    void Because()
    {
        _internalNames = [.. _crafted.Select(store => AzureKeyVaultEncryptionKeyStorage.BuildInternalSecretName(store, InternalSecretPurpose.MutationReceiptDigest))];
        _subjectNames = [];
        _subjectPrefixes = [];
        var subjectComponents = _crafted.Where(component => component.Length > 0).ToArray();
        foreach (var store in subjectComponents)
        {
            foreach (var @namespace in subjectComponents)
            {
                foreach (var identifier in subjectComponents)
                {
                    var locations = SecretLocations.For(store, @namespace, identifier);
                    _subjectPrefixes.Add(locations.Prefix);
                    _subjectNames.Add(locations.ErasureName);
                    _subjectNames.AddRange(_revisions.Select(locations.Name));
                    if ((locations.LegacyPrefix ?? locations.UnresolvedLegacyPrefix) is { } earlierPrefix)
                    {
                        _subjectPrefixes.Add(earlierPrefix);
                        _subjectNames.Add($"{earlierPrefix}{SecretLocations.ErasureSuffix}");
                        _subjectNames.AddRange(_revisions.Select(revision => $"{earlierPrefix}{revision.Value}"));
                    }
                }
            }
        }
    }

    [Fact] void should_never_build_a_subject_name_equal_to_an_internal_name() => _subjectNames.Intersect(_internalNames, StringComparer.OrdinalIgnoreCase).ShouldBeEmpty();
    [Fact] void should_never_build_a_subject_prefix_that_enumerates_an_internal_name() =>
        _subjectPrefixes.Exists(prefix => _internalNames.Exists(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ShouldBeFalse();
    [Fact] void should_start_every_subject_name_with_a_subject_separator() => _subjectNames.TrueForAll(name => name.StartsWith("chronicle-k1--", StringComparison.Ordinal) || name.StartsWith("chronicle--", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_never_start_an_internal_name_with_the_hashed_subject_prefix() => _internalNames.Exists(name => name.StartsWith("chronicle-k1--", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_start_every_internal_name_with_the_internal_segment() => _internalNames.TrueForAll(name => name.StartsWith("chronicle-internal--", StringComparison.Ordinal)).ShouldBeTrue();
}
