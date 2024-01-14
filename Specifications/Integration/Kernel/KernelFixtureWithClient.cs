// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Client;
using Aksio.Cratis.EventSequences;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel;

public class KernelFixtureWithClient : KernelFixture
{
    readonly CancellationTokenSource _cancellationTokenSource = new();

    public KernelFixtureWithClient(GlobalFixture globalFixture) : base(globalFixture)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var webAppBuilder = WebApplication.CreateBuilder()
            .UseCratis(_ => _
                .ForMicroservice(MicroserviceId.Unspecified, "Test"),
                loggerFactory: new NullLoggerFactory());

        webAppBuilder.Logging.ClearProviders();
        var webApp = webAppBuilder.Build();
#pragma warning restore CA2000 // Dispose objects before losing scope
        webApp.UseCratis();

        webApp.StartAsync(_cancellationTokenSource.Token);
        EventLog = webApp.Services.GetRequiredService<IEventLog>();
        Client = webApp.Services.GetRequiredService<IClient>();

        Task.Delay(5000).GetAwaiter().GetResult();
    }

    public IEventLog EventLog { get; }
    public IClient Client { get; }

    public override void Dispose()
    {
        Client.Disconnect().GetAwaiter().GetResult();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        base.Dispose();
    }
}
