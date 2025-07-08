// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.SQL.for_SqlSinkFactory;

public class when_creating_sqlite_sink : Specification
{
    ServiceCollection services;
    ServiceProvider serviceProvider;
    SinkFactory factory;

    void Establish()
    {
        services = new ServiceCollection();
        services.AddSqliteStorage("Data Source=:memory:");
        
        services.AddSingleton<IExpandoObjectConverter, TestExpandoObjectConverter>();
        
        serviceProvider = services.BuildServiceProvider();
    }

    void Because() => factory = serviceProvider.GetService<SinkFactory>();

    [Fact] void should_create_sink_factory() => factory.ShouldNotBeNull();
    [Fact] void should_have_correct_type_id() => factory.TypeId.ShouldEqual(WellKnownSinkTypes.SQL);
}