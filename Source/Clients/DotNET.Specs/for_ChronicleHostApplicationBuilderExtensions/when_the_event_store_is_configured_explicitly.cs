// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_the_event_store_is_configured_explicitly : given.a_host_builder
{
    void Because()
    {
        _builder.AddCratisChronicle(options =>
        {
            options.EventStore = "invoices";
            options.ProgramIdentifier = "billing";
        });
        ResolveOptions();
    }

    [Fact] void should_use_the_configured_event_store() => _options.EventStore.Value.ShouldEqual("invoices");
}
