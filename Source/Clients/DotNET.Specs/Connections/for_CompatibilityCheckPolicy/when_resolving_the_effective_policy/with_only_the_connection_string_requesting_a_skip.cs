// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_CompatibilityCheckPolicy.when_resolving_the_effective_policy;

/// <summary>
/// One setting has to be enough. The untouched options value still defaults to performing the check, so
/// combining the two with AND would swallow this request and go on refusing to connect.
/// </summary>
public class with_only_the_connection_string_requesting_a_skip : Specification
{
    bool _result;

    void Because() => _result = CompatibilityCheckPolicy.ShouldSkip(
        new ChronicleOptions(),
        new ChronicleConnectionString("chronicle://localhost:35000?skipCompatibilityCheck=true"));

    [Fact] void should_skip_the_check() => _result.ShouldBeTrue();
}
