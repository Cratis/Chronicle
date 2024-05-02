// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Reflection;

/// <summary>
/// Represents an implementation of <see cref="ITypeInfo"/>.
/// </summary>
/// <typeparam name="T">Type it holds info for.</typeparam>
public class TypeInfo<T> : ITypeInfo
{
    /// <summary>
    /// Gets a singleton instance of the TypeInfo.
    /// </summary>
    public static readonly TypeInfo<T> Instance = new();

    TypeInfo()
    {
        var type = typeof(T);
        var typeInfo = type.GetTypeInfo();

        var defaultConstructor = typeInfo.DeclaredConstructors.Any(_ => _.GetParameters().Length == 0);

        HasDefaultConstructor =
            typeInfo.IsValueType ||
            defaultConstructor;
    }

    /// <inheritdoc/>
    public bool HasDefaultConstructor { get; }
}
