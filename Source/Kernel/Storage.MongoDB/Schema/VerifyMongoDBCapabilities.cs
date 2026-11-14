// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Rejects explicitly unsupported MongoDB storage services once at startup, outside the append path.
/// </summary>
public class VerifyMongoDBCapabilities : IStartupTask
{
    readonly Func<CancellationToken, Task<MongoDBCapabilityReport>> _probe;
    readonly ILogger<VerifyMongoDBCapabilities> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyMongoDBCapabilities"/> class.
    /// </summary>
    /// <param name="probe">The storage capability probe.</param>
    /// <param name="logger">The logger.</param>
    public VerifyMongoDBCapabilities(IMongoDBCapabilityProbe probe, ILogger<VerifyMongoDBCapabilities> logger)
        : this(probe.Probe, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance with a fakeable inspection callback.
    /// </summary>
    /// <param name="probe">The inspection callback.</param>
    /// <param name="logger">The logger.</param>
    internal VerifyMongoDBCapabilities(Func<CancellationToken, Task<MongoDBCapabilityReport>> probe, ILogger<VerifyMongoDBCapabilities> logger)
    {
        _probe = probe;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await _probe(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var failures = report.Failures.ToArray();
        if (failures.Length > 0)
        {
            throw new MongoDBCapabilitiesNotSupported(failures);
        }

        foreach (var capability in report.Capabilities.Where(capability => capability.Result.State == MongoDBCapabilityState.Inconclusive))
        {
            _logger.CapabilityProbeInconclusive(capability.Name, capability.Result.Reason);
        }
    }
}
