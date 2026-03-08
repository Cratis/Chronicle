// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;
using Orleans.Serialization.Cloning;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// A generalized deep copier for LINQ's internal collection types.
/// This is needed because Orleans doesn't know how to copy these internal LINQ types that are
/// created when using collection expressions like [item] or LINQ methods like .Select(), .Where(), .Take() etc.
/// </summary>
public sealed class LinqCollectionCopier : IGeneralizedCopier, ITypeFilter
{
    /// <inheritdoc/>
    public bool IsSupportedType(Type type)
    {
        // Handle LINQ internal types that implement IEnumerable<T>
        // These have compiler-generated names like <>z__ReadOnlySingleElementList
        if (type.Namespace is null && type.Name.Contains("ReadOnly") && type.Name.Contains("List"))
        {
            return true;
        }

        // Handle collection expression types (e.g., when using [item] syntax)
        if (type.FullName?.Contains("<>z__") == true)
        {
            return true;
        }

        // Handle LINQ iterator types (e.g., IteratorSelectIterator, WhereEnumerableIterator)
        // These are nested classes in System.Linq.Enumerable
        if (type.Namespace == "System.Linq" && type.DeclaringType?.Name == "Enumerable")
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (input is null)
        {
            return null;
        }

        var type = input.GetType();

        // Find the IEnumerable<T> interface to get the element type
        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface is not null)
        {
            var elementType = enumerableInterface.GetGenericArguments()[0];
            var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")!.MakeGenericMethod(elementType);

            // Materialize the collection to an array, which Orleans knows how to handle
            return toArrayMethod.Invoke(null, [input]);
        }

        // Fallback: just return the input if we can't handle it
        return input;
    }

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type) => IsSupportedType(type) ? true : null;
}
