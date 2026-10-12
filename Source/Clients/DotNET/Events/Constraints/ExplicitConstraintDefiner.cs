// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Adapts explicitly nominated plain constraint definers without changing marker-only activation behavior.
/// </summary>
internal static class ExplicitConstraintDefiner
{
    static readonly ConditionalWeakTable<Type, Definer> _definers = new();

    /// <summary>
    /// Defines constraints through the existing marker contract or one exact plain synchronous role.
    /// </summary>
    /// <param name="type">The explicitly nominated definer type.</param>
    /// <param name="instance">The non-disposable activated instance.</param>
    /// <param name="builder">The owning store's constraint builder.</param>
    internal static void Define(Type type, object instance, IConstraintBuilder builder)
    {
        if (instance is IConstraint constraint)
        {
            constraint.Define(builder);
            return;
        }

        _definers.GetValue(type, static artifact => Create(artifact)).Invoke(instance, builder);
    }

    static Definer Create(Type type)
    {
        var roles = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).Where(_ => _.Name == nameof(IConstraint.Define)).ToArray();
        var method = roles.Length == 1 ? roles[0] : null;
        var parameters = method?.GetParameters();
        if (method is not { IsStatic: false } || method.IsGenericMethod || method.ReturnType != typeof(void) ||
            method.IsDefined(typeof(AsyncStateMachineAttribute)) || parameters is not { Length: 1 } ||
            parameters[0].ParameterType != typeof(IConstraintBuilder) || parameters[0].IsOptional || parameters[0].IsDefined(typeof(ParamArrayAttribute)))
        {
            throw new InvalidExplicitArtifact(type, "constraint", "Exactly one public synchronous instance void Define(IConstraintBuilder) role is required.");
        }

        var instance = Expression.Parameter(typeof(object));
        var builder = Expression.Parameter(typeof(IConstraintBuilder));
        var call = Expression.Call(Expression.Convert(instance, type), method, builder);
        return new(Expression.Lambda<Action<object, IConstraintBuilder>>(call, instance, builder).Compile());
    }

    sealed record Definer(Action<object, IConstraintBuilder> Invoke);
}
