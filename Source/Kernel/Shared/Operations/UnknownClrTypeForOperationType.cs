// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Operations;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="OperationType"/> is encountered.
/// </summary>
public class UnknownClrTypeForOperationType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownClrTypeForOperationType"/> class.
    /// </summary>
    /// <param name="type"><see cref="OperationType"/> that has an invalid type identifier.</param>
    public UnknownClrTypeForOperationType(OperationType type)
        : base($"Unknown operation type '{type}'")
    {
    }
}
