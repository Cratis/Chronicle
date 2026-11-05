// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Compliance;

internal static partial class ComplianceAuthorizationLogMessages
{
    [LoggerMessage(LogLevel.Information, "Compliance operation '{Operation}', caller issuer '{CallerIssuer}' subject '{CallerSubject}', namespace '{Namespace}', targets '{Decisions}', mode '{Mode}', listing failed '{ListingFailed}'")]
    internal static partial void ComplianceDecision(this ILogger<ComplianceAuthorization> logger, string callerIssuer, string callerSubject, string operation, string @namespace, string decisions, string mode, bool listingFailed);
}
