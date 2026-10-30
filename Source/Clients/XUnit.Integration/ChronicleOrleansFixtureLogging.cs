// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Holds log messages for <see cref="ChronicleOrleansFixture{TChronicleFixture}"/>.
/// </summary>
/// <typeparam name="TChronicleFixture">The type of the Chronicle fixture.</typeparam>
internal static partial class ChronicleOrleansFixtureLogging<TChronicleFixture>
    where TChronicleFixture : IChronicleFixture
{
    [LoggerMessage(LogLevel.Information, "Fixture kernel reset {ResetDiagnosticId} for invocation '{InvocationDiagnosticId}' starting before storage wipes and grain collection for target '{TargetEventStore}', namespace '{Namespace}'; reset identifier is local and is not propagated to another process")]
    internal static partial void ResetStarting(ILogger<ChronicleOrleansFixture<TChronicleFixture>> logger, Guid resetDiagnosticId, string invocationDiagnosticId, string targetEventStore, string @namespace);

    [LoggerMessage(LogLevel.Information, "Fixture kernel reset {ResetDiagnosticId} for invocation '{InvocationDiagnosticId}' completed both storage wipes, grain collection attempt, storage cache clear and kernel bootstrap for target '{TargetEventStore}', namespace '{Namespace}'; grain collection may have timed out or been unavailable")]
    internal static partial void ResetCompleted(ILogger<ChronicleOrleansFixture<TChronicleFixture>> logger, Guid resetDiagnosticId, string invocationDiagnosticId, string targetEventStore, string @namespace);
}
