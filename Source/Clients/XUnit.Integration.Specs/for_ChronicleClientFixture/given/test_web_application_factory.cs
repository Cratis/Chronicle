// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.Testing;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;

sealed class test_web_application_factory(IServiceProvider services) : IAsyncDisposable
{
    public IServiceProvider Services { get; } = services;

    public HttpClient CreateClient() => new();

    public HttpClient CreateClient(WebApplicationFactoryClientOptions options) => new();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
