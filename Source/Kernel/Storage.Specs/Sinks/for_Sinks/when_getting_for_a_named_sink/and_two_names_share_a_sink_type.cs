// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks.when_getting_for_a_named_sink;

public class and_two_names_share_a_sink_type : given.named_sinks
{
    ISink _reporting;
    ISink _archive;

    async Task Because()
    {
        _reporting = await _sinks.GetFor(ReadModel("reporting"));
        _archive = await _sinks.GetFor(ReadModel("archive"));
    }

    [Fact] void should_give_the_first_its_own_sink() => _reporting.ShouldEqual(_reportingSink);
    [Fact] void should_give_the_second_its_own_sink() => _archive.ShouldEqual(_archiveSink);
}
