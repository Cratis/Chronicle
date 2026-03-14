// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Version;

namespace Cratis.Chronicle.Cli.for_VersionCommand.when_checking_contracts_compatibility;

public class and_major_versions_match : Specification
{
    bool _result;

    void Because() => _result = VersionCommand.AreContractsCompatible("1.2.3", "1.9.0");

    [Fact] void should_be_compatible() => _result.ShouldBeTrue();
}
