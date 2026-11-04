// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.Naming;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBChronicleBuilderExtensions;

public class when_opening_names_that_are_not_allowed : Specification
{
    const string Unreachable = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=300&connectTimeoutMS=300";
    IHost _host;
    Exception _notReady;
    Exception _quarantined;
    bool _readyAtStart;

    void Establish()
    {
        // Nothing listens at the address, so a refusal that comes from the naming rules proves that no connection was needed.
        _host = Host.CreateDefaultBuilder()
            .AddCratisArc()
            .AddCratisMongoDB(options =>
            {
                options.Server = Unreachable;
                options.Database = "chronicle";
            })
            .UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
            .UseOrleans(silo =>
            {
                silo.UseLocalhostClustering();
                silo.Services.AddTypeDiscovery();
                silo.Services.AddBindingsByConvention();
                silo.Services.AddSelfBindings();
                silo.AddChronicleToSilo(builder => builder.WithMongoDB(new ChronicleOptions
                {
                    Storage = new Configuration.Storage { ConnectionDetails = Unreachable }
                }));
            })
            .Build();
    }

    void Because()
    {
        var storage = _host.Services.GetRequiredService<IStorage>();
        var snapshot = _host.Services.GetRequiredService<ResourceNameSnapshot>();
        _readyAtStart = snapshot.IsReady;

        _notReady = Catch.Exception(() => storage.GetEventStore("fresh"));

        snapshot.Publish([new EventStoreName("shop")], []);
        _quarantined = Catch.Exception(() => storage.GetEventStore("shop"));
    }

    [Fact] void should_refuse_a_new_name_until_the_names_are_checked() => _notReady.ShouldBeOfExactType<ResourceNamesNotReady>();
    [Fact] void should_refuse_a_quarantined_name() => _quarantined.ShouldBeOfExactType<ResourceQuarantined>();
    [Fact] void should_start_with_a_snapshot_that_is_not_ready() => _readyAtStart.ShouldBeFalse();
    [Fact] void should_register_the_name_check() => _host.Services.GetRequiredService<ResourceNameAudit>().ShouldNotBeNull();

    void Destroy() => _host?.Dispose();
}
