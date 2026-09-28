// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// The exception that is thrown when an event sequence storage provider cannot persist named tags.
/// </summary>
public class NamedTagsNotSupported : Exception
{
    /// <summary>
    /// Initializes the exception.
    /// </summary>
    public NamedTagsNotSupported() : base("This event sequence storage provider does not support named tags.")
    {
    }
}
