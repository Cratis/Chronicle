// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis;

/// <summary>
/// Represents a default <see cref="IServiceProvider"/> that will create instances of services using the default constructor.
/// </summary>
public class DefaultServiceProvider : IServiceProvider
{
    /// <inheritdoc/>
    public object? GetService(Type serviceType) => Activator.CreateInstance(serviceType);
}
