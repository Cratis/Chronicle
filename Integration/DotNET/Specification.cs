// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

public class Specification(ChronicleConfigurableFixture fixture) : Specification<ChronicleConfigurableFixture>(fixture)
{
    public override bool AutoDiscoverArtifacts => false;
}
