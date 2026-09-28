// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientWebApplicationBuilderExtensions;

public class when_the_web_event_store_is_configured_explicitly : Specification
{
    EventStoreName _eventStore;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<ChronicleClientOptions>(options => options.EventStore = "orders");
        builder.AddCratisChronicle(options => options.EventStore = "invoices");
        using var services = builder.Services.BuildServiceProvider();
        _eventStore = services.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value.EventStore;
    }

    [Fact] void should_keep_the_explicit_web_event_store() => _eventStore.ShouldEqual((EventStoreName)"invoices");
}
