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

    /// <summary>
    /// Configures the Chronicle resource to serve its port with the TLS certificate at the given path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The production image cannot start without this. The Chronicle port serves gRPC (HTTP/2) and the
    /// Workbench, API and OAuth flows (HTTP/1.1) multiplexed through ALPN on a single TLS port, so a
    /// certificate is mandatory — the server throws <c>No TLS certificate is configured</c> at startup
    /// without one. Only the development images generate a self-signed certificate instead of throwing,
    /// which is why the development path works with no certificate at all.
    /// </para>
    /// <para>
    /// Bind-mounts <paramref name="certificatePath"/> read-only into the container at
    /// <see cref="ChronicleContainerImageTags.TlsCertificateContainerPath"/> and sets the
    /// <c>Cratis__Chronicle__Tls__CertificatePath</c> container environment variable to that in-container
    /// path — a path on the host means nothing to the container, so the mount is part of configuring the
    /// certificate rather than something to remember separately. When a
    /// <paramref name="certificatePassword"/> is given, <c>Cratis__Chronicle__Tls__CertificatePassword</c>
    /// is set to it. These map to <c>Cratis:Chronicle:Tls:CertificatePath</c> and
    /// <c>Cratis:Chronicle:Tls:CertificatePassword</c> in the Chronicle server configuration respectively.
    /// </para>
    /// <para>
    /// The certificate must be a PKCS#12 (<c>.pfx</c>) file carrying its private key, and Chronicle reads it
    /// as PKCS#12 only when a password is supplied — pass the password the file was protected with.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="certificatePath">Path on the host to the PKCS#12 certificate file. A relative path resolves against the AppHost directory.</param>
    /// <param name="certificatePassword">Optional password protecting the certificate file.</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    /// <exception cref="CertificateFileDoesNotExist">Thrown when there is no file at <paramref name="certificatePath"/> on the host.</exception>
    /// <example>
    /// <code>
    /// builder.AddCratisChronicle("chronicle", chronicle => chronicle
    ///     .WithMongoDB(mongo)
    ///     .WithTlsCertificate("certs/chronicle.pfx", "YourPassword"));
    /// </code>
    /// </example>
    public static IChronicleAspireBuilder WithTlsCertificate(
        this IChronicleAspireBuilder builder,
        string certificatePath,
        string? certificatePassword = default)
    {
        ConfigureCertificate(
            builder,
            certificatePath,
            certificatePassword,
            ChronicleContainerImageTags.TlsCertificateContainerPath,
            ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable,
            ChronicleContainerImageTags.TlsCertificatePasswordEnvironmentVariable);
        return builder;
    }

    /// <summary>
    /// Configures the Chronicle resource to protect its Data Protection and OAuth keys with the certificate at the given path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The production image needs this whenever it runs its own OAuth authority, which is the default. The
    /// certificate signs and encrypts the internal OAuth authority's keys, and the server throws
    /// <c>An encryption certificate is required in production</c> at startup when it is missing. Turning the
    /// <c>OAuthAuthority</c> feature off, or pointing <c>Authentication:Authority</c> at an external authority,
    /// skips that setup entirely and with it the requirement. The development images fall back to ephemeral
    /// keys instead.
    /// </para>
    /// <para>
    /// Bind-mounts <paramref name="certificatePath"/> read-only into the container at
    /// <see cref="ChronicleContainerImageTags.EncryptionCertificateContainerPath"/> and sets the
    /// <c>Cratis__Chronicle__EncryptionCertificate__CertificatePath</c> container environment variable to that
    /// in-container path. When a <paramref name="certificatePassword"/> is given,
    /// <c>Cratis__Chronicle__EncryptionCertificate__CertificatePassword</c> is set to it. These map to
    /// <c>Cratis:Chronicle:EncryptionCertificate:CertificatePath</c> and
    /// <c>Cratis:Chronicle:EncryptionCertificate:CertificatePassword</c> in the Chronicle server configuration
    /// respectively.
    /// </para>
    /// <para>
    /// The certificate must be a PKCS#12 (<c>.pfx</c>) file carrying its private key. It may be the same file
    /// passed to <see cref="WithTlsCertificate"/> — the two are mounted separately, so pointing both at one
    /// file works.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="certificatePath">Path on the host to the PKCS#12 certificate file. A relative path resolves against the AppHost directory.</param>
    /// <param name="certificatePassword">Optional password protecting the certificate file.</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    /// <exception cref="CertificateFileDoesNotExist">Thrown when there is no file at <paramref name="certificatePath"/> on the host.</exception>
    /// <example>
    /// <code>
    /// builder.AddCratisChronicle("chronicle", chronicle => chronicle
    ///     .WithMongoDB(mongo)
    ///     .WithEncryptionCertificate("certs/encryption.pfx", "YourPassword"));
    /// </code>
    /// </example>
    public static IChronicleAspireBuilder WithEncryptionCertificate(
        this IChronicleAspireBuilder builder,
        string certificatePath,
        string? certificatePassword = default)
    {
        ConfigureCertificate(
            builder,
            certificatePath,
            certificatePassword,
            ChronicleContainerImageTags.EncryptionCertificateContainerPath,
            ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable,
            ChronicleContainerImageTags.EncryptionCertificatePasswordEnvironmentVariable);
        return builder;
    }

    /// <summary>
    /// Bind-mounts a certificate file from the host into the container and points the matching Chronicle
    /// configuration environment variables at the mounted file.
    /// </summary>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="certificatePath">Path on the host to the certificate file, relative to the AppHost directory or absolute.</param>
    /// <param name="certificatePassword">Optional password protecting the certificate file.</param>
    /// <param name="containerPath">Path inside the container to mount the certificate at.</param>
    /// <param name="pathEnvironmentVariable">Environment variable holding the in-container certificate path.</param>
    /// <param name="passwordEnvironmentVariable">Environment variable holding the certificate password.</param>
    /// <exception cref="CertificateFileDoesNotExist">Thrown when there is no file at <paramref name="certificatePath"/> on the host.</exception>
    /// <remarks>
    /// The existence check fails the AppHost at start rather than at container start: Docker creates a
    /// <em>directory</em> at a missing bind-mount source, and the Chronicle container then reports that no
    /// certificate is configured at all — the opposite of what happened. Calling this for the same container
    /// path more than once replaces the earlier mount instead of adding a second one, so the certificate
    /// methods are last-call-wins like every other configuration method on the builder; a second mount on the
    /// same target makes <c>docker run</c> refuse to start the container with a duplicate mount point.
    /// </remarks>
    static void ConfigureCertificate(
        IChronicleAspireBuilder builder,
        string certificatePath,
        string? certificatePassword,
        string containerPath,
        string pathEnvironmentVariable,
        string passwordEnvironmentVariable)
    {
        var resourceBuilder = builder.ResourceBuilder;
        var resolvedPath = Path.GetFullPath(certificatePath, resourceBuilder.ApplicationBuilder.AppHostDirectory);

        if (!File.Exists(resolvedPath))
        {
            throw new CertificateFileDoesNotExist(certificatePath, resolvedPath);
        }

        var existingMounts = resourceBuilder.Resource.Annotations
            .OfType<ContainerMountAnnotation>()
            .Where(_ => _.Target == containerPath)
            .ToArray();

        foreach (var existingMount in existingMounts)
        {
            resourceBuilder.Resource.Annotations.Remove(existingMount);
        }

        resourceBuilder.WithBindMount(resolvedPath, containerPath, isReadOnly: true);
        resourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[pathEnvironmentVariable] = containerPath;

            if (!string.IsNullOrEmpty(certificatePassword))
            {
                context.EnvironmentVariables[passwordEnvironmentVariable] = certificatePassword;
            }
            else
            {
                context.EnvironmentVariables.Remove(passwordEnvironmentVariable);
            }
        });
    }
}
