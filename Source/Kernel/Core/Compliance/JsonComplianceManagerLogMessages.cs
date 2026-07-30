// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Log messages for <see cref="JsonComplianceManager"/>.
/// </summary>
internal static partial class JsonComplianceManagerLogMessages
{
    [LoggerMessage(LogLevel.Error, "Unable to release compliance metadata for property '{PropertyPath}' of '{Identifier}'. The property is surfaced as empty; every other property is unaffected")]
    internal static partial void FailedToReleaseProperty(this ILogger<JsonComplianceManager> logger, string propertyPath, string identifier, Exception exception);
}
