// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleHostApplicationBuilderExtensions;

public class when_the_event_store_is_configured_with_a_dotted_name : given.a_host_builder
{
    void Because()
    {
        _builder.AddCratisChronicle(options => options.EventStore = "Company.Orders.Api");
        ResolveOptions();
    }

    [Fact] void should_leave_the_explicit_name_untouched() => _options.EventStore.Value.ShouldEqual("Company.Orders.Api");
}
