// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_Program;

/// <summary>
/// Specifies that an invalid invocation could not run rather than finding a defect.
/// </summary>
public class when_given_an_unknown_command : Specification
{
    int _exitCode;

    async Task Because() => _exitCode = await Program.Main(["unknown-command"]);

    [Fact] void should_exit_two() => _exitCode.ShouldEqual(2);
}
