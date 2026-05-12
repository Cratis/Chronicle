// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire;

/// <summary>
/// Extension methods for configuring a Chronicle resource via <see cref="IChronicleAspireBuilder"/>.
/// </summary>
public static class ChronicleAspireBuilderExtensions
{
    /// <summary>
    /// Configures the Chronicle resource to use an external MongoDB connection string.
    /// </summary>
    /// <remarks>
    /// Sets the <c>Cratis__Chronicle__Storage__Type</c> container environment variable to <c>MongoDB</c>
    /// and <c>Cratis__Chronicle__Storage__ConnectionDetails</c> to the resolved MongoDB connection string.
    /// These map to <c>Cratis:Chronicle:Storage:Type</c> and <c>Cratis:Chronicle:Storage:ConnectionDetails</c>
    /// in the Chronicle server configuration respectively.
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="mongoDB">The <see cref="IResourceBuilder{T}"/> providing the MongoDB connection string.</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    public static IChronicleAspireBuilder WithMongoDB(
        this IChronicleAspireBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> mongoDB)
    {
        builder.ResourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageTypeEnvironmentVariable] = "MongoDB";
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageConnectionDetailsEnvironmentVariable] = mongoDB.Resource.ConnectionStringExpression;
        });
        return builder;
    }

    /// <summary>
    /// Configures the Chronicle resource to use an external PostgreSQL connection string.
    /// </summary>
    /// <remarks>
    /// Sets the <c>Cratis__Chronicle__Storage__Type</c> container environment variable to <c>PostgreSql</c>
    /// and <c>Cratis__Chronicle__Storage__ConnectionDetails</c> to the resolved PostgreSQL connection string.
    /// These map to <c>Cratis:Chronicle:Storage:Type</c> and <c>Cratis:Chronicle:Storage:ConnectionDetails</c>
    /// in the Chronicle server configuration respectively.
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="postgreSql">The <see cref="IResourceBuilder{T}"/> providing the PostgreSQL connection string.</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    public static IChronicleAspireBuilder WithPostgreSql(
        this IChronicleAspireBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> postgreSql)
    {
        builder.ResourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageTypeEnvironmentVariable] = "PostgreSql";
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageConnectionDetailsEnvironmentVariable] = postgreSql.Resource.ConnectionStringExpression;
        });
        return builder;
    }

    /// <summary>
    /// Configures the Chronicle resource to use an external Microsoft SQL Server connection string.
    /// </summary>
    /// <remarks>
    /// Sets the <c>Cratis__Chronicle__Storage__Type</c> container environment variable to <c>MsSql</c>
    /// and <c>Cratis__Chronicle__Storage__ConnectionDetails</c> to the resolved SQL Server connection string.
    /// These map to <c>Cratis:Chronicle:Storage:Type</c> and <c>Cratis:Chronicle:Storage:ConnectionDetails</c>
    /// in the Chronicle server configuration respectively.
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="msSql">The <see cref="IResourceBuilder{T}"/> providing the SQL Server connection string.</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    public static IChronicleAspireBuilder WithMsSql(
        this IChronicleAspireBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> msSql)
    {
        builder.ResourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageTypeEnvironmentVariable] = "MsSql";
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageConnectionDetailsEnvironmentVariable] = msSql.Resource.ConnectionStringExpression;
        });
        return builder;
    }

    /// <summary>
    /// Configures the Chronicle resource to use a SQLite database with the given connection string.
    /// </summary>
    /// <remarks>
    /// Sets the <c>Cratis__Chronicle__Storage__Type</c> container environment variable to <c>Sqlite</c>
    /// and <c>Cratis__Chronicle__Storage__ConnectionDetails</c> to the provided SQLite connection string.
    /// These map to <c>Cratis:Chronicle:Storage:Type</c> and <c>Cratis:Chronicle:Storage:ConnectionDetails</c>
    /// in the Chronicle server configuration respectively.
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="connectionString">The SQLite connection string (e.g. <c>Data Source=/data/chronicle.db</c>).</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    public static IChronicleAspireBuilder WithSqlite(
        this IChronicleAspireBuilder builder,
        string connectionString)
    {
        builder.ResourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageTypeEnvironmentVariable] = "Sqlite";
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageConnectionDetailsEnvironmentVariable] = connectionString;
        });
        return builder;
    }
}
