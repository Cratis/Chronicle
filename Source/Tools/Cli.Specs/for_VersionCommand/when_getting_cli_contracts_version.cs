// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Version;

namespace Cratis.Chronicle.Cli.for_VersionCommand;

public class when_getting_cli_contracts_version : Specification
{
    string _result;

    void Because() => _result = VersionCommand.GetCliContractsVersion();

    [Fact] void should_return_non_empty_version() => _result.ShouldNotBeEmpty();
    [Fact] void should_not_contain_plus_suffix() => _result.ShouldNotContain("+");
}
