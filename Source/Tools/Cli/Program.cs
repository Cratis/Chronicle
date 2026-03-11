// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Config;
using Cratis.Chronicle.Cli.Commands.Events;
using Cratis.Chronicle.Cli.Commands.EventStores;
using Cratis.Chronicle.Cli.Commands.EventTypes;
using Cratis.Chronicle.Cli.Commands.FailedPartitions;
using Cratis.Chronicle.Cli.Commands.Identities;
using Cratis.Chronicle.Cli.Commands.LlmContext;
using Cratis.Chronicle.Cli.Commands.Namespaces;
using Cratis.Chronicle.Cli.Commands.Observers;
using Cratis.Chronicle.Cli.Commands.Projections;
using Cratis.Chronicle.Cli.Commands.ReadModels;
using Cratis.Chronicle.Cli.Commands.Recommendations;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("cratis");
    config.SetApplicationVersion(typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0");

    config.AddBranch("event-stores", eventStores =>
    {
        eventStores.SetDescription("Manage event stores");
        eventStores.AddCommand<ListEventStoresCommand>("list")
            .WithDescription("List all event stores");
    });

    config.AddBranch("namespaces", namespaces =>
    {
        namespaces.SetDescription("Manage namespaces within an event store");
        namespaces.AddCommand<ListNamespacesCommand>("list")
            .WithDescription("List namespaces in an event store");
    });

    config.AddBranch("event-types", eventTypes =>
    {
        eventTypes.SetDescription("Manage event types");
        eventTypes.AddCommand<ListEventTypesCommand>("list")
            .WithDescription("List registered event types");
    });

    config.AddBranch("events", events =>
    {
        events.SetDescription("Query and inspect events");
        events.AddCommand<GetEventsCommand>("get")
            .WithDescription("Get events from an event sequence");
        events.AddCommand<CountEventsCommand>("count")
            .WithDescription("Get the tail sequence number");
    });

    config.AddBranch("observers", observers =>
    {
        observers.SetDescription("Manage observers (reactors, reducers, projections)");
        observers.AddCommand<ListObserversCommand>("list")
            .WithDescription("List observers");
        observers.AddCommand<ReplayObserverCommand>("replay")
            .WithDescription("Replay an observer from the beginning");
        observers.AddCommand<ReplayPartitionCommand>("replay-partition")
            .WithDescription("Replay a specific partition of an observer");
        observers.AddCommand<RetryPartitionCommand>("retry-partition")
            .WithDescription("Retry a failed partition");
    });

    config.AddBranch("failed-partitions", failedPartitions =>
    {
        failedPartitions.SetDescription("Inspect failed observer partitions");
        failedPartitions.AddCommand<ListFailedPartitionsCommand>("list")
            .WithDescription("List failed partitions");
    });

    config.AddBranch("recommendations", recommendations =>
    {
        recommendations.SetDescription("Manage system recommendations");
        recommendations.AddCommand<ListRecommendationsCommand>("list")
            .WithDescription("List recommendations");
        recommendations.AddCommand<PerformRecommendationCommand>("perform")
            .WithDescription("Perform a recommendation");
        recommendations.AddCommand<IgnoreRecommendationCommand>("ignore")
            .WithDescription("Ignore a recommendation");
    });

    config.AddBranch("identities", identities =>
    {
        identities.SetDescription("Inspect identities");
        identities.AddCommand<ListIdentitiesCommand>("list")
            .WithDescription("List known identities");
    });

    config.AddBranch("projections", projections =>
    {
        projections.SetDescription("Manage projections");
        projections.AddCommand<ListProjectionsCommand>("list")
            .WithDescription("List projection definitions");
        projections.AddCommand<ShowProjectionCommand>("show")
            .WithDescription("Show a projection declaration");
    });

    config.AddBranch("read-models", readModels =>
    {
        readModels.SetDescription("Inspect read model data");
        readModels.AddCommand<ListReadModelsCommand>("list")
            .WithDescription("List read model definitions");
        readModels.AddCommand<GetReadModelInstancesCommand>("instances")
            .WithDescription("List read model instances");
    });

    config.AddBranch("config", configCmd =>
    {
        configCmd.SetDescription("Manage CLI configuration");
        configCmd.AddCommand<ShowConfigCommand>("show")
            .WithDescription("Show current configuration");
        configCmd.AddCommand<SetConfigCommand>("set")
            .WithDescription("Set a configuration value");
        configCmd.AddCommand<ConfigPathCommand>("path")
            .WithDescription("Print configuration file path");
    });

    config.AddCommand<LlmContextCommand>("llm-context")
        .WithDescription("Output CLI capabilities as JSON for AI agent consumption")
        .IsHidden();
});

return await app.RunAsync(args);
