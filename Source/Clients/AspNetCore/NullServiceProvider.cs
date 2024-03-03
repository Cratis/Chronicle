// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.AspNetCore;

/// <summary>
/// Represents a null implementation if <see cref="IServiceProvider"/>.
/// </summary>
internal class NullServiceProvider : IServiceProvider
{
    /// <inheritdoc/>
    public object? GetService(Type serviceType) => null;
}
