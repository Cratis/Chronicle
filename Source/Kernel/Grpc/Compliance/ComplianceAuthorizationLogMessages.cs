// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Compliance;

internal static partial class ComplianceAuthorizationLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Compliance coverage gap in event store '{EventStore}', namespace '{Namespace}', operation '{Operation}'. An unauthorized store retains identifier state; no identifier is logged.")]
    internal static partial void ComplianceCoverageGap(this ILogger<ComplianceAuthorization> logger, string eventStore, string @namespace, string operation);

    [LoggerMessage(LogLevel.Information, "Compliance operation '{Operation}', caller issuer '{CallerIssuer}' subject '{CallerSubject}', namespace '{Namespace}', targets '{Decisions}', mode '{Mode}', listing failed '{ListingFailed}'")]
    internal static partial void ComplianceDecision(this ILogger<ComplianceAuthorization> logger, string callerIssuer, string callerSubject, string operation, string @namespace, string decisions, string mode, bool listingFailed);
}
