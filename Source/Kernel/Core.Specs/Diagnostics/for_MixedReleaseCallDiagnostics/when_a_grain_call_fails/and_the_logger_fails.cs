// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics.when_a_grain_call_fails;

public class and_the_logger_fails : given.a_call_filter
{
    void Establish() => _throwFromLogger = true;
    Task Because() => Invoke();
    [Fact] void should_preserve_the_original_failure() => _failure.ShouldEqual(_original);
}
