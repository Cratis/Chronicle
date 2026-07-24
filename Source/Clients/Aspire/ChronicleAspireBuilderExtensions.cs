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
    const string HashiCorpVaultStorageType = "vault";
    const string AzureKeyVaultStorageType = "azure-key-vault";

    /// <summary>
    /// Configures the Chronicle resource to use an external MongoDB connection string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sets the <c>Cratis__Chronicle__Storage__Type</c> container environment variable to <c>MongoDB</c>
    /// and <c>Cratis__Chronicle__Storage__ConnectionDetails</c> to the resolved MongoDB connection string.
    /// These map to <c>Cratis:Chronicle:Storage:Type</c> and <c>Cratis:Chronicle:Storage:ConnectionDetails</c>
    /// in the Chronicle server configuration respectively.
    /// </para>
    /// <para>
    /// The MongoDB instance must be a replica set. Chronicle relies on MongoDB transactions and change
    /// streams — used by observers, projections, and observable queries — and both require a replica set
    /// rather than a standalone <c>mongod</c>. Against a standalone server the change-stream watch never
    /// opens and observable read-model queries silently return their empty seed, which looks like a
    /// projection bug rather than a storage-topology problem. Aspire's <c>AddMongoDB</c> starts a standalone
    /// <c>mongod</c>, so point <paramref name="mongoDB"/> at a replica set instead — for example MongoDB
    /// Atlas, or a single-node replica-set container that initializes itself.
    /// </para>
    /// <para>
    /// A single-node replica set reached through a host-mapped port must be connected to with
    /// <c>?directConnection=true</c> in the connection string, so the driver does not try to follow the
    /// advertised replica-set member host (only reachable inside the container) back out and hang.
    /// </para>
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

    /// <summary>
    /// Configures the Chronicle resource to use HashiCorp Vault for compliance encryption key storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sets the <c>Cratis__Chronicle__Compliance__Encryption__Storage__Type</c> container environment variable to <c>vault</c>,
    /// <c>Cratis__Chronicle__Compliance__Encryption__Storage__ConnectionDetails</c> to the resolved Vault endpoint URL,
    /// and <c>VAULT_TOKEN</c> to the provided token.
    /// </para>
    /// <para>
    /// These map to <c>Cratis:Chronicle:Compliance:Encryption:Storage:Type</c>,
    /// <c>Cratis:Chronicle:Compliance:Encryption:Storage:ConnectionDetails</c>, and the
    /// <c>VAULT_TOKEN</c> environment variable in the Chronicle server configuration respectively.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="vaultEndpoint">The <see cref="EndpointReference"/> pointing to the HashiCorp Vault HTTP endpoint.</param>
    /// <param name="vaultToken">The Vault authentication token. For development, this is typically the dev root token.</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    public static IChronicleAspireBuilder WithHashiCorpVault(
        this IChronicleAspireBuilder builder,
        EndpointReference vaultEndpoint,
        string vaultToken)
    {
        builder.ResourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ChronicleContainerImageTags.ComplianceEncryptionStorageTypeEnvironmentVariable] = HashiCorpVaultStorageType;
            context.EnvironmentVariables[ChronicleContainerImageTags.ComplianceEncryptionStorageConnectionDetailsEnvironmentVariable] = vaultEndpoint;
            context.EnvironmentVariables[ChronicleContainerImageTags.VaultTokenEnvironmentVariable] = vaultToken;
        });
        return builder;
    }

    /// <summary>
    /// Configures the Chronicle resource to use Azure Key Vault for compliance encryption key storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sets the <c>Cratis__Chronicle__Compliance__Encryption__Storage__Type</c> container environment variable to <c>azure-key-vault</c>
    /// and <c>Cratis__Chronicle__Compliance__Encryption__Storage__ConnectionDetails</c> to the Azure Key Vault URI.
    /// </para>
    /// <para>
    /// Authentication is performed via <c>DefaultAzureCredential</c> on the Chronicle server side.
    /// Ensure the Chronicle container's managed identity or workload identity has the necessary Key Vault permissions.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="keyVaultUri">The Azure Key Vault URI (e.g. <c>https://my-vault.vault.azure.net/</c>).</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    public static IChronicleAspireBuilder WithAzureKeyVault(
        this IChronicleAspireBuilder builder,
        string keyVaultUri)
    {
        builder.ResourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ChronicleContainerImageTags.ComplianceEncryptionStorageTypeEnvironmentVariable] = AzureKeyVaultStorageType;
            context.EnvironmentVariables[ChronicleContainerImageTags.ComplianceEncryptionStorageConnectionDetailsEnvironmentVariable] = keyVaultUri;
        });
        return builder;
    }
}
