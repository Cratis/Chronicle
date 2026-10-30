// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationEnablement;

public class when_the_flag_is_disabled : Specification
{
    bool _enabled;

    void Because() => _enabled = new GenerationEnablement(Options.Create(new ChronicleOptions { Features = new() { ProjectionGenerations = false } })).Enabled;

    [Fact] void should_read_the_existing_feature_flag() => _enabled.ShouldEqual(false);
}
