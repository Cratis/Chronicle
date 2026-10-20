// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_ProvisioningCandidateElection.when_electing;

public class and_some_versions_do_not_take_part : given.versions_of_a_secret
{
    EncryptionKey? _elected;

    void Establish()
    {
        AddVersion("saved-directly", LongAgo.AddMinutes(-10), candidate: false);
        AddVersion("disabled", LongAgo.AddMinutes(-5), enabled: false);
        AddVersion("candidate", LongAgo);
    }

    async Task Because() => _elected = await _election.Elect(SecretName);

    [Fact] void should_elect_only_among_enabled_candidates() => VersionOf(_elected).ShouldEqual("candidate");
}
