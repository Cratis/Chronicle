// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Schema;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Runs a schema action without building a web host or joining Orleans.
/// </summary>
internal static class SchemaCommandRunner
{
    /// <summary>
    /// Determines whether storage has a versioned SQL schema.
    /// </summary>
    /// <param name="type">The storage type.</param>
    /// <returns>Whether the storage type is SQL.</returns>
    internal static bool IsSql(string type) => new[] { StorageType.Sqlite, StorageType.MsSql, StorageType.PostgreSql }
        .Any(candidate => string.Equals(candidate, type, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Runs a one-shot schema action without starting the kernel.
    /// </summary>
    /// <param name="command">The parsed command.</param>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="action">The configured action.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> Run(SchemaCommand command, ChronicleOptions options, SchemaAction action)
    {
        using var logging = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = logging.CreateLogger("Chronicle.Schema");
        var timer = Stopwatch.StartNew();
        SchemaProvisionerLogging.SchemaActionStarting(logger, action.ToString());
        var exit = 0;
        try
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            ConfigureServices(options, services);
            await using var provider = services.BuildServiceProvider();
            if (string.Equals(options.Storage.Type, StorageType.MongoDB, StringComparison.OrdinalIgnoreCase))
            {
                var provisioner = provider.GetRequiredService<IMongoDBIndexProvisioner>();
                exit = await RunMongoAction(provisioner, command, action);
            }
            else if (!IsSql(options.Storage.Type))
            {
                SchemaProvisionerLogging.NoVersionedSchema(logger, options.Storage.Type);
            }
            else
            {
                var provisioner = provider.GetRequiredService<ISchemaProvisioner>();
                var report = action == SchemaAction.VerifyAndExit
                    ? await provisioner.Verify(command.Scope)
                    : await provisioner.Migrate(command.Scope);
                exit = report.IsCurrent ? 0 : 2;
            }
        }
        catch (Exception exception)
        {
            SchemaProvisionerLogging.SchemaActionFailed(logger, exception);
            exit = ExitCode(exception, action);
        }

        SchemaProvisionerLogging.SchemaActionCompleted(logger, action.ToString(), exit, timer.Elapsed);
        return exit;
    }

    /// <summary>
    /// Maps a schema-action failure to its documented exit code.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <param name="action">The schema action.</param>
    /// <returns>The process exit code.</returns>
    internal static int ExitCode(Exception exception, SchemaAction action) =>
        exception is MongoDBIndexesNotProvisioned ? 2 : SchemaExitCode.For(exception, action == SchemaAction.VerifyAndExit);

    /// <summary>
    /// Dispatches a MongoDB schema action and maps an incomplete index report to exit two.
    /// </summary>
    /// <param name="provisioner">The index provisioner.</param>
    /// <param name="command">The parsed command and scopes.</param>
    /// <param name="action">The configured action.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> RunMongoAction(IMongoDBIndexProvisioner provisioner, SchemaCommand command, SchemaAction action)
    {
        var report = action == SchemaAction.VerifyAndExit
            ? await provisioner.Verify(command.Scope.EventStores, command.Scope.Namespaces)
            : await provisioner.Migrate(command.Scope.EventStores, command.Scope.Namespaces);
        return report.IsCurrent ? 0 : 2;
    }

    static void ConfigureServices(ChronicleOptions options, IServiceCollection services)
    {
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(options));
        if (IsSql(options.Storage.Type))
        {
            SqlSchemaServices.Add(services, options);
        }
        else if (string.Equals(options.Storage.Type, StorageType.MongoDB, StringComparison.OrdinalIgnoreCase))
        {
            services.AddCratisArcCore();
            services.AddCratisMongoDB(configureMongoDB: mongo => mongo.WithCamelCaseNamingPolicy());
            BsonSerializer.RegisterSerializationProvider(new ConstraintDefinitionSerializationProvider());
            MongoDBSchemaServices.Add(services, options);
        }
    }
}
