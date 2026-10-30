// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility.for_ProtocolVersionOrder.when_checking_if_known;

public class versions_with_a_known_order : Specification
{
    string[] _versions;
    string[] _unknownVersions;

    void Establish() => _versions = ["19.109.0", "19.110.1-beta.3", "19.110.1+abc"];

    void Because() => _unknownVersions = [.. _versions.Where(version => !ProtocolVersionOrder.IsKnown(version))];

    [Fact] void should_be_known() => _unknownVersions.ShouldBeEmpty();
}
