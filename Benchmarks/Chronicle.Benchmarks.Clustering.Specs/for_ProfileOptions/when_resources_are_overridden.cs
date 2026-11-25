// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.ContainerProfile;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ProfileOptions;

public class when_resources_are_overridden : Specification
{
    ProfileOptions? _options;
    string[] _cells = [];

    void Because()
    {
        (_options, _) = ProfileOptions.Parse(["plan", "--cpus", "4", "--memory", "4g", "--cells", "-server-r2r"]);
        _cells = ProfileMatrix.Select(_options!).Select(cell => cell.Id).ToArray();
    }

    [Fact] void should_use_the_given_cpu_quotas() => _options!.Cpus.ShouldContainOnly(4);
    [Fact] void should_use_the_given_memory_limits() => _options!.Memory.ShouldContainOnly("4g");
    [Fact] void should_select_cells_for_the_given_resources() => _cells.ShouldContainOnly("4cpu-4g-server-r2r");
}
