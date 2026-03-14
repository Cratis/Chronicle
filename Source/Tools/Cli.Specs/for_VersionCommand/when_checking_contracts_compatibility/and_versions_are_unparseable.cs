// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Version;

namespace Cratis.Chronicle.Cli.for_VersionCommand.when_checking_contracts_compatibility;

public class and_versions_are_unparseable : Specification
{
    bool _result;

    void Because() => _result = VersionCommand.AreContractsCompatible("not-a-version", "not-a-version");

    [Fact] void should_fall_back_to_string_equality() => _result.ShouldBeTrue();
}
