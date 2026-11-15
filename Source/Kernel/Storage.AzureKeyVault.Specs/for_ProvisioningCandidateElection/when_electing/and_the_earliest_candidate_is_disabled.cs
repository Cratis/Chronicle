// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_ProvisioningCandidateElection.when_electing;

public class and_the_earliest_candidate_is_disabled : given.versions_of_a_secret
{
    Exception? _error;

    void Establish()
    {
        AddVersion("disabled", LongAgo.AddMinutes(-5), enabled: false);
        AddVersion("candidate", LongAgo);
    }

    async Task Because() => _error = await Catch.Exception(() => _election.Elect(SecretName));

    [Fact] void should_report_the_key_as_unavailable() => _error.ShouldBeOfExactType<EncryptionKeyUnavailable>();
}
