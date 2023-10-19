// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Exception that gets thrown when the content from the reducer is invalid.
/// </summary>
public class InvalidReturnContentFromReducer : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidReturnContentFromReducer"/> class.
    /// </summary>
    /// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> for the response.</param>
    /// <param name="reason">The reason for failing.</param>
    /// <param name="content">The invalid content.</param>
    public InvalidReturnContentFromReducer(HttpStatusCode httpStatusCode, string reason, string content)
        : base($"Invalid content returned from reducer, status code '{httpStatusCode}', with reason phrase '{reason}' and content: '{content}'")
    {
    }
}
