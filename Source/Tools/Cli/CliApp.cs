// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Applications;
using Cratis.Chronicle.Cli.Commands.Auth;
using Cratis.Chronicle.Cli.Commands.Config;
using Cratis.Chronicle.Cli.Commands.Context;
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
using Cratis.Chronicle.Cli.Commands.Users;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Factory for creating a fully configured Chronicle CLI <see cref="CommandApp"/>.
/// </summary>
public static class CliApp
{
    /// <summary>
    /// Creates a new <see cref="CommandApp"/> with all Chronicle CLI commands registered.
    /// </summary>
    /// <returns>A configured <see cref="CommandApp"/> ready to run.</returns>
    public static CommandApp Create()
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("cratis");
            config.SetApplicationVersion(typeof(CliApp).Assembly.GetName().Version?.ToString() ?? "0.0.0");
            config.SetInterceptor(new EventStoreInterceptor());

            config.AddBranch("event-stores", eventStores =>
            {
                eventStores.SetDescription("Manage event stores");
                eventStores.AddCommand<ListEventStoresCommand>("list")
                    .WithDescription("List all event stores")
                    .WithExample("event-stores", "list");
            });

            config.AddBranch("namespaces", namespaces =>
            {
                namespaces.SetDescription("Manage namespaces within an event store");
                namespaces.AddCommand<ListNamespacesCommand>("list")
                    .WithDescription("List namespaces in an event store")
                    .WithExample("namespaces", "list")
                    .WithExample("namespaces", "list", "-e", "MyStore");
            });

            config.AddBranch("event-types", eventTypes =>
            {
                eventTypes.SetDescription("Manage event types");
                eventTypes.AddCommand<ListEventTypesCommand>("list")
                    .WithDescription("List registered event types")
                    .WithExample("event-types", "list");
                eventTypes.AddCommand<ShowEventTypeCommand>("show")
                    .WithDescription("Show an event type registration with its JSON schema")
                    .WithExample("event-types", "show", "UserRegistered")
                    .WithExample("event-types", "show", "UserRegistered+1", "-o", "json");
            });

            config.AddBranch("events", events =>
            {
                events.SetDescription("Query and inspect events");
                events.AddCommand<GetEventsCommand>("get")
                    .WithDescription("Get events from an event sequence")
                    .WithExample("events", "get", "-o", "plain")
                    .WithExample("events", "get", "--from", "100", "--to", "200")
                    .WithExample("events", "get", "--event-type", "UserRegistered");
                events.AddCommand<CountEventsCommand>("count")
                    .WithDescription("Get the tail sequence number")
                    .WithExample("events", "count");
                events.AddCommand<HasEventsCommand>("has")
                    .WithDescription("Check if events exist for an event source ID")
                    .WithExample("events", "has", "abc-123");
            });

            config.AddBranch("observers", observers =>
            {
                observers.SetDescription("Manage observers (reactors, reducers, projections)");
                observers.AddCommand<ListObserversCommand>("list")
                    .WithDescription("List observers")
                    .WithExample("observers", "list")
                    .WithExample("observers", "list", "--type", "reactor");
                observers.AddCommand<ShowObserverCommand>("show")
                    .WithDescription("Show detailed information about a specific observer")
                    .WithExample("observers", "show", "550e8400-e29b-41d4-a716-446655440000");
                observers.AddCommand<ReplayObserverCommand>("replay")
                    .WithDescription("Replay an observer from the beginning")
                    .WithExample("observers", "replay", "550e8400-e29b-41d4-a716-446655440000");
                observers.AddCommand<ReplayPartitionCommand>("replay-partition")
                    .WithDescription("Replay a specific partition of an observer")
                    .WithExample("observers", "replay-partition", "550e8400-e29b-41d4-a716-446655440000", "my-partition");
                observers.AddCommand<RetryPartitionCommand>("retry-partition")
                    .WithDescription("Retry a failed partition")
                    .WithExample("observers", "retry-partition", "550e8400-e29b-41d4-a716-446655440000", "my-partition");
            });

            config.AddBranch("failed-partitions", failedPartitions =>
            {
                failedPartitions.SetDescription("Inspect failed observer partitions");
                failedPartitions.AddCommand<ListFailedPartitionsCommand>("list")
                    .WithDescription("List failed partitions")
                    .WithExample("failed-partitions", "list")
                    .WithExample("failed-partitions", "list", "--observer", "550e8400-e29b-41d4-a716-446655440000");
                failedPartitions.AddCommand<ShowFailedPartitionCommand>("show")
                    .WithDescription("Show detailed information about a specific failed partition")
                    .WithExample("failed-partitions", "show", "550e8400-e29b-41d4-a716-446655440000", "my-partition");
            });

            config.AddBranch("recommendations", recommendations =>
            {
                recommendations.SetDescription("Manage system recommendations");
                recommendations.AddCommand<ListRecommendationsCommand>("list")
                    .WithDescription("List recommendations")
                    .WithExample("recommendations", "list");
                recommendations.AddCommand<PerformRecommendationCommand>("perform")
                    .WithDescription("Perform a recommendation")
                    .WithExample("recommendations", "perform", "550e8400-e29b-41d4-a716-446655440000");
                recommendations.AddCommand<IgnoreRecommendationCommand>("ignore")
                    .WithDescription("Ignore a recommendation")
                    .WithExample("recommendations", "ignore", "550e8400-e29b-41d4-a716-446655440000");
            });

            config.AddBranch("identities", identities =>
            {
                identities.SetDescription("Inspect identities");
                identities.AddCommand<ListIdentitiesCommand>("list")
                    .WithDescription("List known identities")
                    .WithExample("identities", "list", "-o", "plain");
            });

            config.AddBranch("projections", projections =>
            {
                projections.SetDescription("Manage projections");
                projections.AddCommand<ListProjectionsCommand>("list")
                    .WithDescription("List projection definitions")
                    .WithExample("projections", "list");
                projections.AddCommand<ShowProjectionCommand>("show")
                    .WithDescription("Show a projection declaration")
                    .WithExample("projections", "show", "MyProjection", "-o", "json");
            });

            config.AddBranch("read-models", readModels =>
            {
                readModels.SetDescription("Inspect read model data");
                readModels.AddCommand<ListReadModelsCommand>("list")
                    .WithDescription("List read model definitions")
                    .WithExample("read-models", "list");
                readModels.AddCommand<GetReadModelInstancesCommand>("instances")
                    .WithDescription("List read model instances")
                    .WithExample("read-models", "instances", "MyReadModel")
                    .WithExample("read-models", "instances", "MyReadModel", "--page", "2");
                readModels.AddCommand<GetReadModelByKeyCommand>("get")
                    .WithDescription("Get a single read model instance by key")
                    .WithExample("read-models", "get", "MyReadModel", "abc-123");
                readModels.AddCommand<GetReadModelOccurrencesCommand>("occurrences")
                    .WithDescription("List read model occurrences (replay history)")
                    .WithExample("read-models", "occurrences", "MyReadModelType");
                readModels.AddCommand<GetReadModelSnapshotsCommand>("snapshots")
                    .WithDescription("Get snapshots for a read model instance by key")
                    .WithExample("read-models", "snapshots", "MyReadModel", "abc-123");
            });

            config.AddBranch("auth", auth =>
            {
                auth.SetDescription("Authentication management");
                auth.AddCommand<AuthStatusCommand>("status")
                    .WithDescription("Show current authentication status")
                    .WithExample("auth", "status");
            });

            config.AddCommand<LoginCommand>("login")
                .WithDescription("Log in as a user via the password grant flow")
                .WithExample("login", "admin");

            config.AddCommand<LogoutCommand>("logout")
                .WithDescription("Clear the cached login session")
                .WithExample("logout");

            config.AddBranch("context", ctx =>
            {
                ctx.SetDescription("Manage named connection contexts");
                ctx.AddCommand<ListContextsCommand>("list")
                    .WithDescription("List all contexts")
                    .WithExample("context", "list");
                ctx.AddCommand<CreateContextCommand>("create")
                    .WithDescription("Create a new context")
                    .WithExample("context", "create", "dev", "--server", "chronicle://localhost:35000/?disableTls=true")
                    .WithExample("context", "create", "prod", "--server", "chronicle://prod:35000", "-e", "production");
                ctx.AddCommand<SetContextCommand>("set")
                    .WithDescription("Switch to a context")
                    .WithExample("context", "set", "prod");
                ctx.AddCommand<ShowContextCommand>("show")
                    .WithDescription("Show current context details")
                    .WithExample("context", "show");
                ctx.AddCommand<DeleteContextCommand>("delete")
                    .WithDescription("Delete a context")
                    .WithExample("context", "delete", "old-dev");
                ctx.AddCommand<RenameContextCommand>("rename")
                    .WithDescription("Rename a context")
                    .WithExample("context", "rename", "dev", "development");
            });

            config.AddBranch("users", users =>
            {
                users.SetDescription("Manage Chronicle users");
                users.AddCommand<ListUsersCommand>("list")
                    .WithDescription("List all users")
                    .WithExample("users", "list");
                users.AddCommand<AddUserCommand>("add")
                    .WithDescription("Add a new user")
                    .WithExample("users", "add", "alice", "alice@example.com", "P@ssw0rd!");
                users.AddCommand<RemoveUserCommand>("remove")
                    .WithDescription("Remove a user")
                    .WithExample("users", "remove", "550e8400-e29b-41d4-a716-446655440000");
            });

            config.AddBranch("applications", applications =>
            {
                applications.SetDescription("Manage OAuth client applications");
                applications.AddCommand<ListApplicationsCommand>("list")
                    .WithDescription("List all applications")
                    .WithExample("applications", "list");
                applications.AddCommand<AddApplicationCommand>("add")
                    .WithDescription("Add a new application")
                    .WithExample("applications", "add", "my-app", "my-secret");
                applications.AddCommand<RemoveApplicationCommand>("remove")
                    .WithDescription("Remove an application")
                    .WithExample("applications", "remove", "550e8400-e29b-41d4-a716-446655440000");
                applications.AddCommand<RotateSecretCommand>("rotate-secret")
                    .WithDescription("Rotate an application's client secret")
                    .WithExample("applications", "rotate-secret", "550e8400-e29b-41d4-a716-446655440000", "new-secret");
            });

            config.AddBranch("config", configCmd =>
            {
                configCmd.SetDescription("Manage CLI configuration");
                configCmd.AddCommand<ShowConfigCommand>("show")
                    .WithDescription("Show current configuration")
                    .WithExample("config", "show");
                configCmd.AddCommand<SetConfigCommand>("set")
                    .WithDescription("Set a configuration value")
                    .WithExample("config", "set", "server", "chronicle://myhost:35000");
                configCmd.AddCommand<ConfigPathCommand>("path")
                    .WithDescription("Print configuration file path")
                    .WithExample("config", "path");
            });

            config.AddCommand<LlmContextCommand>("llm-context")
                .WithDescription("Output CLI capabilities as JSON for AI agent consumption")
                .WithExample("llm-context");
        });

        return app;
    }
}
