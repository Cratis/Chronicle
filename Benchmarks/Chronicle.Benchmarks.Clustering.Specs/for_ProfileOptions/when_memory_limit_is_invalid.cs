// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.ContainerProfile;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ProfileOptions;

public class when_memory_limit_is_invalid : Specification
{
    ProfileOptions? _options;
    string? _error;

    void Because() => (_options, _error) = ProfileOptions.Parse(["plan", "--memory", "4gb"]);

    [Fact] void should_not_produce_options() => _options.ShouldBeNull();
    [Fact] void should_name_the_option() => _error.ShouldEqual("Invalid option or value: --memory.");
}
