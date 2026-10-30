// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_only_payload_contracts_are_given : Specification
{
    Options _options;

    void Because() => _options = Options.Parse(["--payload-baseline", "previous.txt", "--payload-current", "payload.txt"]);

    [Fact] void should_read_the_current_payload_path() => _options.PayloadCurrent.ShouldEqual("payload.txt");
    [Fact] void should_read_the_baseline_payload_path() => _options.PayloadBaseline.ShouldEqual("previous.txt");
    [Fact] void should_not_require_grain_contracts() => _options.Current.ShouldBeNull();
}
