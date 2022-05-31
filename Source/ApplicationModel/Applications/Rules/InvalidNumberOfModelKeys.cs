// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Exception that gets thrown when there are an invalid number of model keys on a model. Only one is allowed.
/// </summary>
public class InvalidNumberOfModelKeys : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidNumberOfModelKeys"/> class.
    /// </summary>
    /// <param name="type">Type that is invalid.</param>
    /// <param name="properties">Properties that has model key.</param>
    public InvalidNumberOfModelKeys(Type type, IEnumerable<PropertyInfo> properties)
        : base($"Invalid number of model keys on '{type.FullName}'. Only one allowed. (Keys = {string.Join(",", properties.Select(_ => _.Name))}) ")
    {
    }
}
