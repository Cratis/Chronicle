// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_ProvisioningCandidateElection.when_electing;

/// <summary>
/// The first listing was taken before the only candidate it saw had settled, but delivered after; meanwhile a racer
/// wrote a candidate in the same second that sorts first. The clock reads one second after creation until the first
/// listing is delivered and three seconds after from then on. The election must not accept the stale listing.
/// </summary>
public class and_a_listing_is_delivered_after_its_winner_settled : given.versions_of_a_secret
{
    static readonly DateTimeOffset _created = LongAgo;

    EncryptionKey? _result;
    int _listings;

    void Establish()
    {
        var first = SecretModelFactory.SecretProperties(name: SecretName, version: "b-first", createdOn: _created);
        first.Enabled = true;
        first.Tags[ProvisioningCandidateElection.CandidateTag] = bool.TrueString;
        var racer = SecretModelFactory.SecretProperties(name: SecretName, version: "a-racer", createdOn: _created);
        racer.Enabled = true;
        racer.Tags[ProvisioningCandidateElection.CandidateTag] = bool.TrueString;

        _secretClient.GetPropertiesOfSecretVersionsAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(_ => AsyncPageable<SecretProperties>.FromPages([Page<SecretProperties>.FromValues(
                _listings++ == 0 ? [first] : [first, racer],
                null,
                Substitute.For<Response>())]));

        var clock = new listing_clock(() => _listings == 0 ? _created + TimeSpan.FromSeconds(1) : _created + TimeSpan.FromSeconds(3));
        _election = new(_secretClient, clock, value => new EncryptionKey(System.Text.Encoding.UTF8.GetBytes(value), []));
    }

    async Task Because() => _result = await _election.Elect(SecretName);

    [Fact] void should_elect_the_candidate_that_sorts_first() => VersionOf(_result).ShouldEqual("a-racer");
    [Fact] void should_list_the_versions_again() => _listings.ShouldEqual(2);

    sealed class listing_clock(Func<DateTimeOffset> now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now();
    }
}
