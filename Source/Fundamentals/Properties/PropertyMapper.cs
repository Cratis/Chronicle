// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Represents the delegate of an operation that maps data from a source into a target object.
    /// </summary>
    /// <typeparam name="TSource">Type of the source.</typeparam>
    /// <typeparam name="TTarget">Type of the target.</typeparam>
    /// <param name="source">Source object.</param>
    /// <param name="target"><see cref="ExpandoObject"/> target to write to.</param>
    /// <param name="arrayIndexers"><see cref="IArrayIndexers"/> for accessing nested objects with arrays.</param>
    public delegate void PropertyMapper<TSource, TTarget>(TSource source, TTarget target, IArrayIndexers arrayIndexers);
}
