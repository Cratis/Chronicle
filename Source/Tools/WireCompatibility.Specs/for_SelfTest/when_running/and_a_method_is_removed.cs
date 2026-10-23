// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;

namespace Cratis.Chronicle.Tools.WireCompatibility.for_SelfTest.when_running;

public class and_a_method_is_removed : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = SelfTest.Check(Path.Combine(AppContext.BaseDirectory, "chronicle.desc"), WireIncompatibilityKind.MethodRemoved);

    [Fact] void should_detect_the_planted_break() => _result.Incompatibilities.Single().Kind.ShouldEqual(WireIncompatibilityKind.MethodRemoved);
    [Fact] void should_reject_the_contract() => _result.IsCompatible.ShouldBeFalse();
}
