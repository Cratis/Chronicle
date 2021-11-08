// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Properties
{
    /// <summary>
    /// Represents the delegate of an operation that maps data from a source into a target object.
    /// </summary>
    /// <typeparam name="T">Type of the source.</typeparam>
    /// <param name="source">Source object.</param>
    /// <param name="target"><see cref="ExpandoObject"/> target to write to.</param>
    public delegate void PropertyMapper<T>(T source, ExpandoObject target);
}
