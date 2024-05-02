// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

SelfBindingRegistrationSource.AddNamespaceStartsWithToExclude(
    "Microsoft",
    "Orleans");

var host = Cratis.Kernel.Server.Program.CreateHostBuilder(Array.Empty<string>())
          .Build();

GlobalVariables.SetServiceProvider(host!.Services);

await host.StartAsync();

await Task.Delay(1000);

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, new DebugInProcessConfig());
