// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a bulk window is lost before its writes can be confirmed.
/// </summary>
public class BulkWindowLost : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkWindowLost"/> class.
    /// </summary>
    public BulkWindowLost()
        : base("A bulk window was lost before its writes could be confirmed durable.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkWindowLost"/> class with the reason the window was lost.
    /// </summary>
    /// <param name="reason">The reason the bulk window was lost.</param>
    public BulkWindowLost(string reason)
        : base($"A bulk window was lost before its writes could be confirmed durable: {reason}")
    {
    }
}
