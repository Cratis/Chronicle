// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Version;

namespace Cratis.Chronicle.Cli.for_VersionCommand.when_checking_contracts_compatibility;

public class and_versions_have_prerelease_suffixes : Specification
{
    bool _result;

    void Because() => _result = VersionCommand.AreContractsCompatible("1.0.0-preview.1", "1.5.0-rc.2");

    [Fact] void should_be_compatible() => _result.ShouldBeTrue();
}
