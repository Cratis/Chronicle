// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// The address an earlier secret name belongs to, once it could be tied to exactly one.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/>.</param>
/// <param name="EventStoreNamespace">The <see cref="EventStoreNamespaceName"/>.</param>
/// <param name="Identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
/// <param name="IsErasure">Whether the name is the address' erasure record.</param>
/// <param name="Revision">The revision, when the name is a key.</param>
internal record LegacySecretName(
    EventStoreName EventStore,
    EventStoreNamespaceName EventStoreNamespace,
    EncryptionKeyIdentifier Identifier,
    bool IsErasure,
    uint Revision)
{
    const string EarlierPrefix = "chronicle--";
    const string Separator = "--";
    const string SanitizedSubjectPrefix = "chronicle-encrypted-value-subject-";
    const int ComponentCount = 4;

    /// <summary>
    /// Gets the name the secret is copied to.
    /// </summary>
    public string Destination
    {
        get
        {
            var locations = SecretLocations.For(EventStore, EventStoreNamespace, Identifier);
            return IsErasure ? locations.ErasureName : locations.Name(Revision);
        }
    }

    /// <summary>
    /// Gets the name of the erasure record of the address, under the current names.
    /// </summary>
    public string DestinationErasure => SecretLocations.For(EventStore, EventStoreNamespace, Identifier).ErasureName;

    /// <summary>
    /// Gets the name of the erasure record of the address, under the earlier names.
    /// </summary>
    public string? SourceErasure => SecretLocations.For(EventStore, EventStoreNamespace, Identifier).LegacyErasureName;

    /// <summary>
    /// Gets whether a secret name is one an earlier version may have written.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <returns>True when it is.</returns>
    public static bool IsEarlier(string name) => name.StartsWith(EarlierPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Ties an earlier secret name to the one address it was made from, using the scopes an operator listed.
    /// </summary>
    /// <remarks>
    /// Earlier names are lowercased and reduced to letters, digits and hyphens, so the address cannot be read off the name. Every
    /// candidate address built from the listed scopes is named again the way an earlier version named it, and the name is only
    /// tied to an address when exactly one candidate gives the same name.
    /// </remarks>
    /// <param name="name">The secret name.</param>
    /// <param name="scopes">The listed event store and namespace pairs.</param>
    /// <returns>The <see cref="LegacySecretName"/>, or null when the name cannot be tied to exactly one address.</returns>
    public static LegacySecretName? Resolve(string name, IReadOnlyList<LegacyScope> scopes)
    {
        if (!IsEarlier(name))
        {
            return null;
        }

        var parts = name[EarlierPrefix.Length..].Split(Separator);
        if (parts.Length != ComponentCount || parts.Any(part => part.Length == 0))
        {
            return null;
        }

        var suffix = parts[3];
        var isErasure = string.Equals(suffix, SecretLocations.ErasureSuffix, StringComparison.OrdinalIgnoreCase);
        if (!isErasure && !uint.TryParse(suffix, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return null;
        }

        var revision = isErasure ? 0u : uint.Parse(suffix, System.Globalization.CultureInfo.InvariantCulture);
        var prefix = name[..(name.Length - suffix.Length)];
        var matches = Candidates(scopes, parts[2])
            .Where(candidate => string.Equals(SecretNames.LegacyPrefix(candidate.EventStore, candidate.EventStoreNamespace, candidate.Identifier), prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .Take(2)
            .ToList();

        return matches.Count == 1
            ? new(matches[0].EventStore, matches[0].EventStoreNamespace, matches[0].Identifier, isErasure, revision)
            : null;
    }

    static IEnumerable<Address> Candidates(IReadOnlyList<LegacyScope> scopes, string identifierPart)
    {
        var all = scopes
            .Concat(scopes.Select(scope => scope with { EventStoreNamespace = EncryptedValueKeyIdentifiers.GlobalNamespace.Value }))
            .Append(new(EncryptedValueKeyIdentifiers.GlobalEventStore.Value, EncryptedValueKeyIdentifiers.GlobalNamespace.Value))
            .Distinct();

        var identifiers = new List<string>
        {
            identifierPart,
            EncryptedValueKeyIdentifiers.ForNamespace().Value,
            EncryptedValueKeyIdentifiers.ForGlobal().Value
        };
        if (identifierPart.StartsWith(SanitizedSubjectPrefix, StringComparison.Ordinal))
        {
            identifiers.Add(EncryptedValueKeyIdentifiers.ForSubject(identifierPart[SanitizedSubjectPrefix.Length..]).Value);
        }

        return all.SelectMany(scope => identifiers.Select(identifier => new Address(scope.EventStore, scope.EventStoreNamespace, identifier)));
    }

    record Address(EventStoreName EventStore, EventStoreNamespaceName EventStoreNamespace, EncryptionKeyIdentifier Identifier);
}
