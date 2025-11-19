// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

public class Specification(ChronicleOutOfProcessFixtureWithLocalImage fixture) : Specification<ChronicleOutOfProcessFixtureWithLocalImage>(fixture)
{
    public override bool AutoDiscoverArtifacts => false;
}
