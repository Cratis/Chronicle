// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_ProvisioningCandidateElection.when_electing;

public class and_there_are_no_candidates : given.versions_of_a_secret
{
    EncryptionKey? _elected;

    void Establish() => AddVersion("saved-directly", LongAgo, candidate: false);

    async Task Because() => _elected = await _election.Elect(SecretName);

    [Fact] void should_elect_nothing() => _elected.ShouldBeNull();
}
