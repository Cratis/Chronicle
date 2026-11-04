// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The exception that is thrown when the names in use could not be read within the time budget.
/// </summary>
/// <param name="timeout">The time budget that ran out.</param>
/// <param name="inner">The last failure, if any.</param>
public class ResourceNameAuditFailed(TimeSpan timeout, Exception? inner) : Exception($"The names in use could not be read within {timeout}.", inner);
