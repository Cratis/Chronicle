// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_CompatibilityCheckPolicy.when_resolving_the_effective_policy;

public class with_only_the_options_requesting_a_skip : Specification
{
    bool _result;

    void Because() => _result = CompatibilityCheckPolicy.ShouldSkip(
        new ChronicleOptions { SkipCompatibilityCheck = true },
        new ChronicleConnectionString("chronicle://localhost:35000"));

    [Fact] void should_skip_the_check() => _result.ShouldBeTrue();
}
