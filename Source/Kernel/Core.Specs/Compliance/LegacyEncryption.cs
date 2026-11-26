// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Compliance;

/// <summary>Creates an explicitly pre-floor writer for legacy-envelope regression scenarios.</summary>
internal static class LegacyEncryption
{
    internal static Encryption Create()
    {
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(new ClusterReleaseSummary([(new SemanticVersion(19, 178, 0), 1)], 0, 0));
        return new Encryption(releases);
    }
}
