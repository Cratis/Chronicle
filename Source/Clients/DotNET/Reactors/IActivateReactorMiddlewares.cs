// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a system for activating <see cref="IReactorMiddlewares"/> from the <see cref="IServiceProvider"/>.
/// </summary>
public interface IActivateReactorMiddlewares
{
    /// <summary>
    /// Activates all the middlewares.
    /// </summary>
    /// <param name="scopedServiceProvider">The <see cref="IServiceProvider"/> to resolve the middlewares.</param>
    /// <returns>The activated <see cref="IReactorMiddlewares"/>.</returns>
    IReactorMiddlewares Activate(IServiceProvider scopedServiceProvider);
}
