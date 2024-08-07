// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Exception that gets thrown when no unit of work has been started.
/// </summary>
public class NoUnitOfWorkHasBeenStarted() : Exception("No unit of work has been started");
