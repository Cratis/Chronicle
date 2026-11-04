// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.ContainerProfile;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ProfileOptions;

public class when_cleanup_has_no_session : Specification
{
    ProfileOptions? _options;
    string? _error;

    void Because() => (_options, _error) = ProfileOptions.Parse(["cleanup"]);

    [Fact] void should_not_produce_executable_options() => _options.ShouldBeNull();
    [Fact] void should_require_a_session() => _error.ShouldEqual("cleanup requires --session <id>.");
}
