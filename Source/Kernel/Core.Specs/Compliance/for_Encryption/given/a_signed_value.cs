// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Compliance.for_Encryption.given;

public class a_signed_value : Specification
{
    protected static readonly ProtectedValueAddress _address = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject");
    protected Encryption _encryption;
    protected Storage.Compliance.EncryptionKey _key;
    protected byte[] _encrypted;

    protected static Encryption CreateEncryptionAt(SemanticVersion release)
    {
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(new ClusterReleaseSummary([(release, 1)], 0, 0));
        return new(releases, new());
    }

    void Establish()
    {
        _encryption = CreateEncryptionAt(ClusterReleaseBoundary.KeyIdentity);
        _key = _encryption.GenerateKey();
        _encrypted = _encryption.Encrypt("value"u8.ToArray(), _key, 7, _address);
    }
}
