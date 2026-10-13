// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

internal static partial class LanguageServiceLogging
{
    [LoggerMessage(LogLevel.Warning, "The events capture setting 'sequence {Sequence}' is deprecated. Use 'from <Event>' lines only; the inbox is derived from the events' origin")]
    internal static partial void DeprecatedSequenceSetting(this ILogger<LanguageService> logger, string sequence);
}
