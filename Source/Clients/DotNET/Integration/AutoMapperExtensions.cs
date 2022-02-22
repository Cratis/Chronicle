// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Reflection;

namespace AutoMapper;

/// <summary>
/// Extension methods for working with automapper.
/// </summary>
public static class AutoMapperExtensions
{
    /// <summary>
    /// Map member for a record type.
    /// </summary>
    /// <param name="mappingExpression">Mapping expression to map for.</param>
    /// <param name="destinationMember">The destination member we're mapping towards.</param>
    /// <param name="sourceMember">The source member we're mapping from.</param>
    /// <typeparam name="TSource">Source type to map from.</typeparam>
    /// <typeparam name="TDestination">Destination type to map to.</typeparam>
    /// <typeparam name="TMember">Type of member being mapped.</typeparam>
    /// <returns>Mapping expression for fluent continuation.</returns>
    public static IMappingExpression<TSource, TDestination> MapMember<TSource, TDestination, TMember>(
        this IMappingExpression<TSource, TDestination> mappingExpression,
        Expression<Func<TDestination, TMember>> destinationMember,
        Expression<Func<TSource, TMember>> sourceMember)
    {
        var memberInfo = destinationMember.GetPropertyInfo();
        var memberName = memberInfo.Name;
        return mappingExpression
            .ForMember(destinationMember, opt => opt.MapFrom(sourceMember));
    }

    /// <summary>
    /// Map member for a record type.
    /// </summary>
    /// <param name="mappingExpression">Mapping expression to map for.</param>
    /// <param name="destinationMember">The destination member we're mapping towards.</param>
    /// <param name="sourceMember">The source member we're mapping from.</param>
    /// <typeparam name="TSource">Source type to map from.</typeparam>
    /// <typeparam name="TDestination">Destination type to map to.</typeparam>
    /// <typeparam name="TMember">Type of member being mapped.</typeparam>
    /// <returns>Mapping expression for fluent continuation.</returns>
    /// <remarks>
    /// Inspired and adapted from here: https://stackoverflow.com/a/67398323.
    /// </remarks>
    public static IMappingExpression<TSource, TDestination> MapRecordMember<TSource, TDestination, TMember>(
        this IMappingExpression<TSource, TDestination> mappingExpression,
        Expression<Func<TDestination, TMember>> destinationMember,
        Expression<Func<TSource, TMember>> sourceMember)
        where TDestination : IEquatable<TDestination>
    {
        var memberInfo = destinationMember.GetPropertyInfo();
        var memberName = memberInfo.Name;
        return mappingExpression
            .ForMember(destinationMember, opt => opt.MapFrom(sourceMember))
            .ForCtorParam(memberName, opt => opt.MapFrom(sourceMember));
    }
}
