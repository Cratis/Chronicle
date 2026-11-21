// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad;

public class when_classifying_silo_stopping_appends : Specification
{
    const string CommandFailed = "Cratis.Chronicle.Contracts.Commands.CommandFailed";

    [Fact]
    public void should_treat_a_silo_stopping_refusal_as_uncertain() =>
        ClusterLoad.IsSiloStoppingAppend(CommandFailed, "Command failed: Silo stopping").ShouldBeTrue();

    [Theory]
    [InlineData("Command failed: An unrelated command failure")]
    [InlineData("Command failed: Silo started")]
    [InlineData("Silo stopping")]
    public void should_not_treat_other_command_failures_as_silo_stopping(string message) =>
        ClusterLoad.IsSiloStoppingAppend(CommandFailed, message).ShouldBeFalse();

    [Fact]
    public void should_not_treat_other_exception_types_as_silo_stopping() =>
        ClusterLoad.IsUncertainAppend(new InvalidOperationException("Command failed: Silo stopping")).ShouldBeFalse();
}
