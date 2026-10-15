// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a bulk window is lost before its writes can be confirmed.
/// </summary>
public class BulkWindowLost() : Exception("A bulk window was lost before its writes could be confirmed durable.");
