// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

internal static partial class NamespaceIncarnationLogging
{
    [LoggerMessage(LogLevel.Error, "Namespace {Namespace} requires explicit recovery: {Failure}")]
    internal static partial void NamespaceRecoveryRequired(this ILogger logger, EventStoreNamespaceName @namespace, string? failure);
}
