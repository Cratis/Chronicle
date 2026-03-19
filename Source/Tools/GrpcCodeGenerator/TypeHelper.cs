// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Helper methods for working with types during code generation.
/// </summary>
public static class TypeHelper
{
    /// <summary>
    /// Gets the primitive type if the type is a ConceptAs&lt;T&gt;, otherwise returns the type itself.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The unwrapped primitive type or the original type.</returns>
    public static Type UnwrapConceptType(Type type)
    {
        var current = type;
        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("ConceptAs`", StringComparison.Ordinal))
            {
                return current.GetGenericArguments()[0];
            }

            current = current.BaseType;
        }

        return type;
    }

    /// <summary>
    /// Checks if a type is or derives from ConceptAs&lt;T&gt;.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is a concept type.</returns>
    public static bool IsConceptType(Type type)
    {
        var current = type;
        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("ConceptAs`", StringComparison.Ordinal))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Checks if a type is an observable type (ISubject&lt;T&gt; or IObservable&lt;T&gt;).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is an observable type.</returns>
    public static bool IsObservableType(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var name = genericDef.Name;
            if (name.StartsWith("ISubject`", StringComparison.Ordinal) ||
                name.StartsWith("IObservable`", StringComparison.Ordinal) ||
                name.StartsWith("BehaviorSubject`", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            (i.GetGenericTypeDefinition().Name.StartsWith("ISubject`", StringComparison.Ordinal) ||
             i.GetGenericTypeDefinition().Name.StartsWith("IObservable`", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Gets the inner type argument of Task&lt;T&gt;, IObservable&lt;T&gt;, ISubject&lt;T&gt;, etc.
    /// </summary>
    /// <param name="type">The wrapper type.</param>
    /// <returns>The inner type or null.</returns>
    public static Type? GetInnerType(Type type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        return type.GetGenericArguments().FirstOrDefault();
    }

    /// <summary>
    /// Determines if a type is a Task (non-generic).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it is Task.</returns>
    public static bool IsVoidTask(Type type) => type == typeof(Task);

    /// <summary>
    /// Gets the actual return type for a query method, unwrapping Task&lt;T&gt;.
    /// </summary>
    /// <param name="returnType">The method return type.</param>
    /// <returns>The inner type if wrapped in Task, the observable inner type, or the type itself.</returns>
    public static Type GetQueryReturnType(Type returnType)
    {
        if (IsObservableType(returnType))
        {
            return returnType;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return returnType.GetGenericArguments()[0];
        }

        return returnType;
    }

    /// <summary>
    /// Checks if a type implements IOneOf from the OneOf library.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it implements IOneOf.</returns>
    public static bool IsOneOfType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition().FullName?.StartsWith("OneOf.OneOf`", StringComparison.Ordinal) == true)
        {
            return true;
        }

        return type.GetInterfaces().Any(i => i.Name == "IOneOf");
    }

    /// <summary>
    /// Gets the first type argument from a OneOf type (the success type).
    /// </summary>
    /// <param name="type">The OneOf type.</param>
    /// <returns>The first type argument.</returns>
    public static Type GetOneOfSuccessType(Type type)
    {
        if (type.IsGenericType)
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    /// <summary>
    /// Gets the C# type name suitable for use in generated code.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The C# type name string.</returns>
    public static string GetTypeName(Type type)
    {
        if (type == typeof(void))
        {
            return "void";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(double))
        {
            return "double";
        }

        if (type == typeof(float))
        {
            return "float";
        }

        if (type == typeof(decimal))
        {
            return "decimal";
        }

        if (type == typeof(Guid))
        {
            return "Guid";
        }

        if (type == typeof(DateTimeOffset))
        {
            return "DateTimeOffset";
        }

        if (type == typeof(DateTime))
        {
            return "DateTime";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericDef = type.GetGenericTypeDefinition();
        var genericArgs = type.GetGenericArguments();

        if (genericDef == typeof(Nullable<>))
        {
            return $"{GetTypeName(genericArgs[0])}?";
        }

        if (genericDef.FullName?.StartsWith("System.Collections.Generic.IEnumerable`", StringComparison.Ordinal) == true)
        {
            return $"IEnumerable<{GetTypeName(genericArgs[0])}>";
        }

        if (genericDef == typeof(Task<>))
        {
            return $"Task<{GetTypeName(genericArgs[0])}>";
        }

        if (genericDef.FullName?.StartsWith("System.IObservable`", StringComparison.Ordinal) == true ||
            genericDef.Name.StartsWith("IObservable`", StringComparison.Ordinal))
        {
            return $"IObservable<{GetTypeName(genericArgs[0])}>";
        }

        if (genericDef.Name.StartsWith("ISubject`", StringComparison.Ordinal) ||
            genericDef.Name.StartsWith("BehaviorSubject`", StringComparison.Ordinal))
        {
            return $"IObservable<{GetTypeName(genericArgs[0])}>";
        }

        var baseName = genericDef.Name;
        var backtickIndex = baseName.IndexOf('`');
        if (backtickIndex >= 0)
        {
            baseName = baseName[..backtickIndex];
        }

        var argNames = string.Join(", ", genericArgs.Select(GetTypeName));
        return $"{baseName}<{argNames}>";
    }

    /// <summary>
    /// Checks whether a type is a read model type (has ReadModel attribute).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if it has the ReadModel attribute.</returns>
    public static bool IsReadModelType(Type type) =>
        type.GetCustomAttributesData().Any(a => a.AttributeType.Name == "ReadModelAttribute");
}
