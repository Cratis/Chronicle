// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

internal static partial class FederationConsumerKeeperLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed to reconcile the federation consumers with the bindings")]
    internal static partial void ReconcileFailed(this ILogger<FederationConsumerKeeper> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to reach the federation consumer of binding '{BindingId}'")]
    internal static partial void EnsureFailed(this ILogger<FederationConsumerKeeper> logger, string bindingId, Exception exception);
}
