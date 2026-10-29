// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Clustering;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Sys;
using Microsoft.Extensions.Logging;
using Orleans.Serialization;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Names mixed-release call failures without recording payloads, exception text or grain identities.
/// </summary>
/// <param name="clusterReleases">The cluster release summary provider.</param>
/// <param name="logger">The bounded failure logger.</param>
internal sealed class MixedReleaseCallDiagnostics(IClusterReleases clusterReleases, ILogger<MixedReleaseCallDiagnostics> logger) : IOutgoingGrainCallFilter
{
    readonly ConcurrentDictionary<(Type? Interface, string Method), byte> _reported = new();
    bool _suppressed;

    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        try
        {
            await context.Invoke();
        }
        catch (Exception exception) when (Observe(context, exception))
        {
            throw;
        }
    }

    static bool IgnoreDiagnosticFailure(Exception exception) => exception is not null;

    static string FailureType(Exception exception)
    {
        if (exception is TypeLoadException) return nameof(TypeLoadException);
        if (exception is CodecNotFoundException) return nameof(CodecNotFoundException);
        if (exception is UnsupportedWireTypeException) return nameof(UnsupportedWireTypeException);
        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                if (IncompatibleSiloRelease.IsLikely(inner)) return FailureType(inner);
            }
        }
        return exception.InnerException is { } wrapped ? FailureType(wrapped) : "unknown";
    }

    static bool IsChronicle(Type? type) => type?.Namespace is { } ns &&
        (ns == "Cratis.Chronicle" || ns.StartsWith("Cratis.Chronicle.", StringComparison.Ordinal));

    bool Observe(IOutgoingGrainCallContext context, Exception exception)
    {
        try
        {
            if (!IncompatibleSiloRelease.IsLikely(exception)) return false;
            var method = context.InterfaceMethod;
            var type = method.DeclaringType;
            if (!TryAdmit(type, method.Name)) return false;
            var isChronicle = IsChronicle(type);
            var interfaceName = isChronicle ? type!.Name.Split('`')[0] : "external";
            logger.MixedReleaseCallFailed(interfaceName, isChronicle ? method.Name : "external", FailureType(exception), KernelVersion.Current.ToString(), clusterReleases.Summarize().ToString());
        }
        catch (Exception failure) when (IgnoreDiagnosticFailure(failure))
        {
            // Diagnostics must never replace the original call failure.
        }
        return false;
    }

    bool TryAdmit(Type? interfaceType, string method)
    {
        var key = (interfaceType, method);
        lock (_reported)
        {
            if (_reported.ContainsKey(key)) return false;
            if (_reported.Count < 32) return _reported.TryAdd(key, 0);
            if (!_suppressed)
            {
                _suppressed = true;
                logger.FurtherFailuresSuppressed();
            }
            return false;
        }
    }
}
