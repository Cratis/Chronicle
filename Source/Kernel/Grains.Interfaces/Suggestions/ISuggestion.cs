// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Defines a system for a suggestion in the system.
/// </summary>
public interface ISuggestion : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Perform the suggestion.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Perform();

    /// <summary>
    /// Ignore the suggestion.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ignore();
}

/// <summary>
/// Defines a system for a suggestion in the system.
/// </summary>
/// <typeparam name="TRequest">Type of request for the task.</typeparam>
public interface ISuggestion<TRequest> : ISuggestion
    where TRequest : class
{
    /// <summary>
    /// Initialize the suggestion.
    /// </summary>
    /// <param name="description">The description of the suggestion.</param>
    /// <param name="request">Request to set.</param>
    /// <returns>Awaitable task.</returns>
    Task Initialize(SuggestionDescription description, TRequest request);
}
