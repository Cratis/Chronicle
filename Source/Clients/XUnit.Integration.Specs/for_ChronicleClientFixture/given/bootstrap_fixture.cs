// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;

public class bootstrap_fixture(IChronicleFixture fixture) : ChronicleClientFixture<IChronicleFixture>(fixture)
{
    public List<string> Steps { get; } = [];

    protected override IAsyncDisposable CreateWebApplicationFactory() => throw new InvalidOperationException("The lifecycle spec must not create a host");

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
    }

    protected override Task OnBeforeInitializeAsync()
    {
        Steps.Add("bootstrap-and-reset");
        return Task.CompletedTask;
    }

    protected override Task OnInitializeAsync()
    {
        Steps.Add("scenario");
        return Task.CompletedTask;
    }

    protected override Task OnDisposeAsync()
    {
        Steps.Add("teardown");
        return Task.CompletedTask;
    }
}
