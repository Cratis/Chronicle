// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Exception that gets thrown when the content from the reducer is invalid.
/// </summary>
public class InvalidReturnContentFromReducer : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidReturnContentFromReducer"/> class.
    /// </summary>
    /// <param name="content">The invalid content.</param>
    public InvalidReturnContentFromReducer(string content)
        : base($"Invalid content from reducer: 'content'")
    {
    }
}
