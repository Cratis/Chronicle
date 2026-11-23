// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_many_namespaces_register_at_once.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// A burst of registrations from many namespaces, while every application of a registration takes a while, is answered
/// at once and applied by a bounded number of calls to the grain that owns them.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(BusyKernelCollection.Name)]
public class when_many_namespaces_register_at_once(context _context) : IClassFixture<context>
{
    public const int Namespaces = 14;
    public const int RegistrationsPerNamespace = 6;
    public const int Limit = 4;

    public class context(BusyKernelClusteringFixture fixture) : IAsyncLifetime
    {
        public TimeSpan SlowestAnswer { get; private set; }
        public int Failures { get; private set; }
        public int MostApplying { get; private set; }
        public int StillPending { get; private set; }
        public TimeSpan TimeToApplyAll { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.RegisterAll();
            var services = ((IChronicleServicesAccessor)eventStore.Connection).Services;
            var definitions = await services.Projections.GetAllDefinitions(new() { EventStore = eventStore.Name.Value });

            fixture.Busy.Hold = TimeSpan.FromMilliseconds(100);
            var applying = Stopwatch.StartNew();
            var answers = await Task.WhenAll(Enumerable.Range(0, Namespaces).SelectMany(@namespace => Enumerable.Range(0, RegistrationsPerNamespace).Select(async index =>
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await services.Projections.Register(new RegisterRequest
                    {
                        EventStore = eventStore.Name.Value,
                        Owner = ProjectionOwner.Client,
                        Projections = [.. definitions],
                        Registrant = $"burst-{@namespace}",
                        Version = $"1.{index}",
                        FullSet = false
                    });
                    return (stopwatch.Elapsed, Failed: false);
                }
                catch (Exception)
                {
                    return (stopwatch.Elapsed, Failed: true);
                }
            })));
            SlowestAnswer = answers.Max(_ => _.Elapsed);
            Failures = answers.Count(_ => _.Failed);

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(120);
            while (DateTime.UtcNow < deadline)
            {
                var states = await Task.WhenAll(Enumerable.Range(0, Namespaces).Select(@namespace =>
                    services.Projections.GetRegistrationStatus(new() { EventStore = eventStore.Name.Value, Registrant = $"burst-{@namespace}" })));
                StillPending = states.Count(_ => _.State == RegistrationState.Pending);
                if (StillPending == 0)
                {
                    break;
                }

                // Task.Delay: poll interval while waiting for every namespace to be projected, bounded by the deadline.
                await Task.Delay(200);
            }

            TimeToApplyAll = applying.Elapsed;
            MostApplying = fixture.Busy.MostApplying;
            fixture.Busy.Hold = TimeSpan.Zero;

            if (Environment.GetEnvironmentVariable("REGISTRATION_BURST_LOG") is { Length: > 0 } log)
            {
                await File.AppendAllTextAsync(log, $"registrations={Namespaces * RegistrationsPerNamespace} namespaces={Namespaces} slowestAnswerMs={SlowestAnswer.TotalMilliseconds:0} mostApplying={MostApplying} timeToApplyAllMs={TimeToApplyAll.TotalMilliseconds:0} failures={Failures}{Environment.NewLine}");
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_answer_every_registration() => _context.Failures.ShouldEqual(0);
    [Fact] void should_answer_well_within_the_call_timeout() => _context.SlowestAnswer.ShouldBeLessThan(TimeSpan.FromSeconds(10));
    [Fact] void should_apply_with_no_more_than_the_limit_in_flight() => _context.MostApplying.ShouldBeLessThanOrEqual(Limit);
    [Fact] void should_apply_every_registration() => _context.StillPending.ShouldEqual(0);
}
