// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// A read model member carrying projection attributes - either a record constructor parameter or a property -
/// seen through one shape so both are processed by the same code.
/// </summary>
internal sealed class ProjectedMember
{
    readonly ParameterInfo? _parameter;
    readonly MemberInfo? _member;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectedMember"/> class for a constructor parameter.
    /// </summary>
    /// <param name="parameter">The constructor <see cref="ParameterInfo"/>.</param>
    internal ProjectedMember(ParameterInfo parameter) => _parameter = parameter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectedMember"/> class for a property.
    /// </summary>
    /// <param name="member">The <see cref="MemberInfo"/>.</param>
    internal ProjectedMember(MemberInfo member) => _member = member;

    /// <summary>
    /// Gets the name of the member.
    /// </summary>
    internal string Name => _parameter is not null ? _parameter.Name! : _member!.Name;

    /// <summary>
    /// Gets the member as an <see cref="ICustomAttributeProvider"/>.
    /// </summary>
    internal ICustomAttributeProvider Provider => (ICustomAttributeProvider?)_parameter ?? _member!;

    /// <summary>
    /// Gets the declared type of the member.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the member is neither a parameter nor a property.</exception>
    internal Type MemberType => _parameter is not null
        ? _parameter.ParameterType
        : (_member as PropertyInfo ?? throw new InvalidOperationException("Expected PropertyInfo")).PropertyType;

    /// <summary>
    /// Gets a value indicating whether the member is marked <see cref="NestedAttribute"/>.
    /// </summary>
    internal bool IsNested => _parameter is not null
        ? _parameter.IsDefined(typeof(NestedAttribute), inherit: false)
        : Attribute.IsDefined(_member!, typeof(NestedAttribute));

    /// <summary>
    /// Gets all the custom attributes of the member.
    /// </summary>
    /// <returns>The attributes.</returns>
    internal IEnumerable<Attribute> GetAttributes() => _parameter is not null
        ? _parameter.GetCustomAttributes()
        : _member!.GetCustomAttributes();

    /// <summary>
    /// Gets a single custom attribute of the given type.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute.</typeparam>
    /// <returns>The attribute, or null when the member does not carry it.</returns>
    internal TAttribute? GetAttribute<TAttribute>()
        where TAttribute : Attribute => _parameter is not null
            ? _parameter.GetCustomAttribute<TAttribute>()
            : _member!.GetCustomAttribute<TAttribute>();

    /// <summary>
    /// Throws when a clear is declared for this member and it cannot hold null.
    /// </summary>
    /// <exception cref="CannotClearNonNullableMember">Thrown when the member cannot hold null.</exception>
    internal void ThrowIfCannotHoldNull()
    {
        if (_parameter is not null)
        {
            ScalarClear.ThrowIfCannotHoldNull(_parameter.Member.DeclaringType!, _parameter);
            return;
        }

        ScalarClear.ThrowIfCannotHoldNull(_member!.DeclaringType!, _member);
    }
}
