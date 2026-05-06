// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.for_ChronicleOptions.when_getting_default_sink_type_id;

public class with_default_options : Specification
{
    ChronicleOptions _options;

    void Because() => _options = new ChronicleOptions();

    [Fact] void should_default_to_mongodb() => _options.DefaultSinkTypeId.ShouldEqual(WellKnownSinkTypes.MongoDB);
}
