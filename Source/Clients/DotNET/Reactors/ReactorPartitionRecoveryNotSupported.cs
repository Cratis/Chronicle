// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// The exception that is thrown when a reactor registry does not provide partition recovery.
/// </summary>
public class ReactorPartitionRecoveryNotSupported : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorPartitionRecoveryNotSupported"/> class.
    /// </summary>
    public ReactorPartitionRecoveryNotSupported()
        : base("This reactor registry does not support failed partition retry.")
    {
    }
}
