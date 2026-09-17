// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_CompatibilityCheckPolicy.when_resolving_the_effective_policy;

/// <summary>
/// The out-of-the-box case: a client refuses to connect to a server it is not compatible with unless
/// someone deliberately says otherwise.
/// </summary>
public class without_a_request_to_skip_in_either_input : Specification
{
    bool _result;

    void Because() => _result = CompatibilityCheckPolicy.ShouldSkip(
        new ChronicleOptions(),
        new ChronicleConnectionString("chronicle://localhost:35000"));

    [Fact] void should_perform_the_check() => _result.ShouldBeFalse();
}
