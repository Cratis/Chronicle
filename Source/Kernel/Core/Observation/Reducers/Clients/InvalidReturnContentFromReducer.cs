// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Exception that gets thrown when the content from the reducer is invalid.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidReturnContentFromReducer"/> class.
/// </remarks>
/// <param name="httpStatusCode">The <see cref="HttpStatusCode"/> for the response.</param>
/// <param name="reason">The reason for failing.</param>
/// <param name="content">The invalid content.</param>
public class InvalidReturnContentFromReducer(HttpStatusCode httpStatusCode, string reason, string content) : Exception($"Invalid content returned from reducer, status code '{httpStatusCode}', with reason phrase '{reason}' and content: '{content}'")
{
}
