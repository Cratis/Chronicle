// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

internal static partial class NamespaceCasingLogging
{
    [LoggerMessage(LogLevel.Information, "Namespace '{Requested}' in event store '{EventStore}' is canonicalized to '{Canonical}'")]
    internal static partial void Canonicalized(this ILogger<NamespaceCasing> logger, string eventStore, string requested, string canonical);
}
