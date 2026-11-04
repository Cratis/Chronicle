// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.ContainerProfile;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_DockerCli;

public class when_executing_an_unsafe_command : Specification
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => DockerCli.Run(
        new DockerCommand("unsafe command", ["system", "prune"], TimeSpan.FromSeconds(30)), CancellationToken.None));

    [Fact] void should_reject_before_starting_docker() => _error.ShouldBeOfExactType<HarnessFailure>();
}
