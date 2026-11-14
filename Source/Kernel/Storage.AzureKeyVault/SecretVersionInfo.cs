// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// The attributes of a secret, or of one of its versions. Holds no value.
/// </summary>
/// <param name="Name">The secret name.</param>
/// <param name="Version">The version id.</param>
/// <param name="CreatedOn">When the version was created.</param>
/// <param name="Enabled">Whether the version is enabled, or null when not stated.</param>
/// <param name="ExpiresOn">When the version expires.</param>
/// <param name="NotBefore">When the version becomes usable.</param>
/// <param name="Tags">The tags.</param>
/// <param name="ContentType">The content type.</param>
public record SecretVersionInfo(
    string Name,
    string Version,
    DateTimeOffset? CreatedOn,
    bool? Enabled,
    DateTimeOffset? ExpiresOn,
    DateTimeOffset? NotBefore,
    IReadOnlyDictionary<string, string> Tags,
    string? ContentType)
{
    /// <summary>
    /// Gets whether the version carries anything beyond a plain, enabled value. Chronicle's own provisioning marker is not counted.
    /// </summary>
    public bool HasAttributes =>
        Enabled == false ||
        ExpiresOn is not null ||
        NotBefore is not null ||
        !string.IsNullOrEmpty(ContentType) ||
        Tags.Keys.Any(tag => tag != ProvisioningCandidateElection.CandidateTag);
}
