// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.AspNetCore;

/// <summary>
/// Represents a class with global instances.
/// </summary>
internal static class GlobalInstances
{
    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> to use.
    /// </summary>
    internal static IServiceProvider ServiceProvider { get; set; } = new NullServiceProvider();
}
