// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Elects the one key every provisioner and reader agrees on when several provisioners wrote candidates for the same
/// secret, because Azure Key Vault has no conditional write that could have stopped all but one of them.
/// </summary>
/// <remarks>
/// <para>
/// Every write to a secret adds a version and Key Vault keeps them all, so no racer's material is lost - the race is
/// only about which version everyone uses. The winner is the oldest enabled candidate by creation time, tie-broken
/// ordinally on the version identifier, which every caller computes the same way from the same versions.
/// </para>
/// <para>
/// An election is only final once the winner is older than <see cref="SettleTime"/>. Creation times are stamped at
/// one-second granularity, so a concurrent write landing in the same second can be invisible to one listing and
/// still sort first in the next. Waiting until the winner has settled means every write that could sort before it
/// has landed and is visible, so the winner can no longer change.
/// </para>
/// </remarks>
/// <param name="secretClient">The <see cref="SecretClient"/> to read versions through.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> elections are settled against.</param>
/// <param name="deserialize">Turns a secret value into the <see cref="EncryptionKey"/> it holds.</param>
internal sealed class ProvisioningCandidateElection(SecretClient secretClient, TimeProvider timeProvider, Func<string, EncryptionKey?> deserialize)
{
    /// <summary>
    /// The tag marking a secret version as a provisioning candidate that takes part in the election.
    /// </summary>
    internal const string CandidateTag = "chronicle-provisioning-candidate";

    /// <summary>
    /// How long a candidate has to have existed before its election is final.
    /// </summary>
    /// <remarks>
    /// One second of creation-time granularity plus a second of clock skew between the vault and the silo.
    /// </remarks>
    internal static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The most times an election waits for its winner to settle before it accepts the winner it has.
    /// </summary>
    /// <remarks>
    /// A settled winner cannot be displaced, so a second round already ends every election whose clocks agree; the
    /// bound only stops a vault reporting creation times in the future from holding a caller indefinitely.
    /// </remarks>
    const int MaximumSettleRounds = 3;

    /// <summary>
    /// Mark a secret about to be written as a provisioning candidate.
    /// </summary>
    /// <param name="secret">The <see cref="KeyVaultSecret"/> to mark.</param>
    public static void MarkAsCandidate(KeyVaultSecret secret) => secret.Properties.Tags[CandidateTag] = bool.TrueString;

    /// <summary>
    /// Check whether a secret version is a provisioning candidate.
    /// </summary>
    /// <param name="properties">The <see cref="SecretProperties"/> of the version.</param>
    /// <returns>True if it takes part in the election; false if it was written any other way.</returns>
    public static bool IsCandidate(SecretProperties properties) =>
        properties.Tags.TryGetValue(CandidateTag, out var value) &&
        string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Elect the key every caller agrees on for a secret.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>The elected <see cref="EncryptionKey"/>, or <see langword="null"/> when no candidate can be read.</returns>
    public async Task<EncryptionKey?> Elect(string name)
    {
        for (var round = 0; ; round++)
        {
            var elected = await FindWinner(name);
            if (elected is null)
            {
                return null;
            }

            var remaining = (elected.CreatedOn ?? DateTimeOffset.MinValue) + SettleTime - timeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero || round == MaximumSettleRounds)
            {
                return await ReadVersion(name, elected.Version);
            }

            await Task.Delay(remaining, timeProvider);
        }
    }

    static bool SortsBefore(SecretProperties candidate, SecretProperties elected)
    {
        var candidateCreated = candidate.CreatedOn ?? DateTimeOffset.MaxValue;
        var electedCreated = elected.CreatedOn ?? DateTimeOffset.MaxValue;
        return candidateCreated < electedCreated ||
            (candidateCreated == electedCreated && string.CompareOrdinal(candidate.Version, elected.Version) < 0);
    }

    async Task<SecretProperties?> FindWinner(string name)
    {
        SecretProperties? elected = null;
        try
        {
            await foreach (var version in secretClient.GetPropertiesOfSecretVersionsAsync(name))
            {
                if (version.Enabled == true && IsCandidate(version) && (elected is null || SortsBefore(version, elected)))
                {
                    elected = version;
                }
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }

        return elected;
    }

    async Task<EncryptionKey?> ReadVersion(string name, string version)
    {
        try
        {
            return deserialize((await secretClient.GetSecretAsync(name, version)).Value.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
