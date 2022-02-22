// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aksio.Cratis.Applications.Queries;

/// <summary>
/// Defines an observable that is observed by a connected client.
/// </summary>
public interface IClientObservable
{
    /// <summary>
    /// Handle the action context and result from the action.
    /// </summary>
    /// <param name="context"><see cref="ActionExecutingContext"/> to handle for.</param>
    /// <param name="jsonOptions">The <see cref="JsonOptions"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleConnection(ActionExecutingContext context, JsonOptions jsonOptions);

    /// <summary>
    /// Get an async enumerator for the observable.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Async enumerator.</returns>
    object GetAsynchronousEnumerator(CancellationToken cancellationToken = default);
}
