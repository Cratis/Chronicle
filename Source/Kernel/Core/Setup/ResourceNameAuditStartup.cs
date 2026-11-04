// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Checks the names in use while the silo starts, before the services that depend on them are available.
/// </summary>
/// <param name="audit">The <see cref="ResourceNameAudit"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="ChronicleOptions"/>.</param>
/// <param name="storageErrorClassifier">The <see cref="IStorageErrorClassifier"/> recognizing the storage provider's transient failures.</param>
public class ResourceNameAuditStartup(
    ResourceNameAudit audit,
    IOptions<ChronicleOptions> options,
    IStorageErrorClassifier storageErrorClassifier) : ILifecycleParticipant<ISiloLifecycle>
{
    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(ResourceNameAuditStartup), ServiceLifecycleStage.ApplicationServices, Run);

    Task Run(CancellationToken cancellationToken) =>
        audit.RunAsync(
            options.Value.Storage.NameCollisions,
            options.Value.Storage.NameAuditTimeout,
            exception => SiblingSiloInstability.IsTransient(exception) || storageErrorClassifier.IsTransient(exception),
            cancellationToken);
}
