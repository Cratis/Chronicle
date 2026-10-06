// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance.GDPR;

internal static partial class PIICompliancePropertyValueHandlerLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Treating an unrecoverable protected value for subject '{Subject}' as erased because it cannot be unwrapped with the current key and erasure is recorded.")]
    internal static partial void UnrecoverableValueAfterErasure(this ILogger<PIICompliancePropertyValueHandler> logger, string subject);
}
