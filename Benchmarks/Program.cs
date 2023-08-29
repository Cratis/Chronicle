// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Autofac;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

SelfBindingRegistrationSource.AddNamespaceStartsWithToExclude(
    "Microsoft",
    "Orleans");

var host = Aksio.Cratis.Kernel.Server.Program.CreateHostBuilder(Array.Empty<string>())
    .Build();

GlobalVariables.SetServiceProvider(host.Services);

await host.StartAsync();

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, new DebugInProcessConfig());
