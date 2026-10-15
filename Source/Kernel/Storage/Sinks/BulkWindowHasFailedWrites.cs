// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a bulk window cannot confirm durability after a write failed.
/// </summary>
public class BulkWindowHasFailedWrites() : Exception("The bulk window contains failed writes whose outcomes have already been claimed");
