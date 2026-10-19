// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_ProvisioningCandidateElection.given;

/// <summary>
/// A secret whose versions are served by a substitute <see cref="SecretClient"/>, each holding a key whose public
/// part is the version's identifier, so the elected key says which version won.
/// </summary>
public class versions_of_a_secret : Specification
{
    protected const string SecretName = "chronicle--store--ns--identifier--1";
    protected static readonly DateTimeOffset LongAgo = DateTimeOffset.UtcNow.AddHours(-1);

    protected SecretClient _secretClient;
    internal ProvisioningCandidateElection _election;
    protected List<SecretProperties> _versions = [];

    void Establish()
    {
        _secretClient = Substitute.For<SecretClient>();
        _secretClient.GetPropertiesOfSecretVersionsAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(_ => AsyncPageable<SecretProperties>.FromPages([Page<SecretProperties>.FromValues(_versions, null, Substitute.For<Response>())]));
        _secretClient.GetSecretAsync(SecretName, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Response.FromValue(
                SecretModelFactory.KeyVaultSecret(SecretModelFactory.SecretProperties(name: SecretName, version: call.ArgAt<string>(1)), call.ArgAt<string>(1)),
                Substitute.For<Response>()));
        _election = new(_secretClient, TimeProvider.System, value => new EncryptionKey(System.Text.Encoding.UTF8.GetBytes(value), []));
    }

    protected void AddVersion(string version, DateTimeOffset createdOn, bool candidate = true, bool enabled = true)
    {
        var properties = SecretModelFactory.SecretProperties(name: SecretName, version: version, createdOn: createdOn);
        properties.Enabled = enabled;
        if (candidate)
        {
            properties.Tags[ProvisioningCandidateElection.CandidateTag] = bool.TrueString;
        }

        _versions.Add(properties);
    }

    protected static string VersionOf(EncryptionKey? key) => key is null ? string.Empty : System.Text.Encoding.UTF8.GetString(key.Public);
}
