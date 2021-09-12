// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// using Cratis.Events;
// using Cratis.Events.Observation;
// using Cratis.Events.Store.Grpc.Contracts;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;

// namespace Cratis
// {
//     [EventType("bca45a99-5f56-4f4e-acf7-f086de4dd72b", 1)]
//     public record MyEvent(string TheString, int TheInt);

//     [Observer("1fffd3fc-af75-4eb8-bf00-9fd453e11e20")]
//     public class MyObserver
//     {
//         void DoStuff(MyEvent @event)
//         {
//             //return Task.CompletedTask;
//         }
//     }

//     public static class Program
//     {
//         public static async Task Main()
//         {
//             using var host = new HostBuilder()
//                                     .UseCratis("275ae482-7458-40c3-9fc0-4138bd3a0ee3")
//                                     .ConfigureServices(_ => _
//                                         .AddTransient<MyObserver>()
//                                         .AddLogging(lb => lb.AddConsole()))
//                                     .Build();

//             await host.RunAsync();
//             /*await host.StartAsync();

//             await host.Services.GetService<IEventStore>()
//                     .Commit(
//                         "d577ccae-2912-4240-bea5-4bb9a2a782f8",
//                         new MyEvent("Hello", 42)
//                     );*/
//         }
//     }
// }
