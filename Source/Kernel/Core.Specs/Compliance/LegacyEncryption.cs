// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Compliance;

/// <summary>Creates an explicitly pre-floor writer for legacy-envelope regression scenarios.</summary>
internal static class LegacyEncryption
{
    internal static Encryption Create() => CreateAt(new(19, 178, 0));

    internal static Encryption CreateBeforeTypedFloor() => CreateAt(new(19, 195, 0));

    internal static Encryption CreateAt(SemanticVersion release)
    {
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(new ClusterReleaseSummary([(release, 1)], 0, 0));
        return new Encryption(releases);
    }
}
