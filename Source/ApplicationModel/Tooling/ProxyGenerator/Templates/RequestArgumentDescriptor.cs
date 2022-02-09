// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator.Templates
{
    /// <summary>
    /// Describes a query argument for templating purposes.
    /// </summary>
    /// <param name="Name">Name of argument.</param>
    /// <param name="Type">Type of argument.</param>
    /// <param name="IsOptional">Whether or not the argument is nullable / optional.</param>
    public record RequestArgumentDescriptor(string Name, string Type, bool IsOptional);
}
