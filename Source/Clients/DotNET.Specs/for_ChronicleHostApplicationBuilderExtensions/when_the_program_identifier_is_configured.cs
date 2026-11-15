// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_the_program_identifier_is_configured : given.a_host_builder
{
    void Because()
    {
        _builder.AddCratisChronicle(options => options.ProgramIdentifier = "billing");
        ResolveOptions();
    }

    [Fact] void should_default_the_event_store_to_the_program_identifier() => _options.EventStore.Value.ShouldEqual("billing");
}
