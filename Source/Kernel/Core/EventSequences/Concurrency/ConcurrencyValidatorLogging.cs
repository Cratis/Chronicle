// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.EventSequences.Concurrency;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ConcurrencyValidatorLogging
{
    [LoggerMessage(LogLevel.Debug, "Skipping the concurrency check for event source '{EventSourceId}' - the append declared a concurrency scope that carries no expected sequence number, so there is nothing to compare against and the append proceeds unchecked")]
    internal static partial void SkippingIncompleteConcurrencyScope(this ILogger<ConcurrencyValidator> logger, EventSourceId eventSourceId);
}
