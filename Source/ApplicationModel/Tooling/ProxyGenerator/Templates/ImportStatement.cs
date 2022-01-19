// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator.Templates
{
    /// <summary>
    /// Describes an import statement.
    /// </summary>
    /// <param name="Type">Type to use.</param>
    /// <param name="Module">Source module in which the type is from.</param>
    public record ImportStatement(string Type, string Module);
}
