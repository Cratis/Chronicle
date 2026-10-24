// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_ProvisioningCandidateElection.when_electing;

public class and_candidates_were_written_concurrently : given.versions_of_a_secret
{
    EncryptionKey? _elected;

    void Establish()
    {
        AddVersion("later", LongAgo.AddSeconds(1));
        AddVersion("tied-b", LongAgo);
        AddVersion("tied-a", LongAgo);
    }

    async Task Because() => _elected = await _election.Elect(SecretName);

    [Fact] void should_elect_the_oldest_candidate_breaking_ties_on_the_version() => VersionOf(_elected).ShouldEqual("tied-a");
}
