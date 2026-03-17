// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle;

/// <summary>
/// Extensions for <see cref="ChronicleOptions"/>.
/// </summary>
public static class ChronicleOptionsExtensions
{
    /// <summary>
    /// Sets the <see cref="INamingPolicy"/> to use CamelCase naming.
    /// </summary>
    /// <param name="options"><see cref="ChronicleOptions"/> to set the naming policy on.</param>
    /// <returns>The same <see cref="ChronicleOptions"/> instance with the CamelCase naming policy set.</returns>
    public static ChronicleOptions WithCamelCaseNamingPolicy(this ChronicleOptions options)
    {
        options.NamingPolicy = new CamelCaseNamingPolicy();
        return options;
    }
}
