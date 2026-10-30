// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility.for_ProtocolVersionOrder.when_checking_if_known;

public class versions_without_a_known_order : Specification
{
    string[] _versions;
    string[] _knownVersions;

    void Establish() => _versions = [null!, string.Empty, "   ", "1.0.0", "1.0.0.0", "1.0.0+abc123", "1.0.0-local", "garbage", "19.109", "19.109.invalid"];

    void Because() => _knownVersions = [.. _versions.Where(ProtocolVersionOrder.IsKnown)];

    [Fact] void should_not_be_known() => _knownVersions.ShouldBeEmpty();
}
