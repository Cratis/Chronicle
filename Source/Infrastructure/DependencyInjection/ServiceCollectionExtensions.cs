// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Execution;
using Cratis.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    static readonly string[] _namespacesToIgnoreForSelfBinding = ["System", "Microsoft"];

    /// <summary>
    /// Add service bindings by convention.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddBindingsByConvention(this IServiceCollection services)
    {
        var types = Types.Types.Instance;

        static bool convention(Type i, Type t) => i.Namespace == t.Namespace && i.Name == $"I{t.Name}";

        var conventionBasedTypes = types!.All.Where(_ =>
        {
            var interfaces = _.GetInterfaces();
            if (interfaces.Length > 0)
            {
                var conventionInterface = interfaces.SingleOrDefault(i => convention(i, _));
                if (conventionInterface != default)
                {
                    return types!.All.Count(type => type.HasInterface(conventionInterface)) == 1;
                }
            }
            return false;
        });

        foreach (var conventionBasedType in conventionBasedTypes)
        {
            var interfaceToBind = types.All.Single(_ => _.IsInterface && convention(_, conventionBasedType));
            if (services.Any(_ => _.ServiceType == interfaceToBind) || conventionBasedType.IsAbstract)
            {
                continue;
            }

            _ = conventionBasedType.HasAttribute<SingletonAttribute>() ?
                services.AddSingleton(interfaceToBind, conventionBasedType) :
                services.AddTransient(interfaceToBind, conventionBasedType);
        }

        return services;
    }

    /// <summary>
    /// Add self bindings for types that are not already registered.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddSelfBindings(this IServiceCollection services)
    {
        const TypeAttributes staticType = TypeAttributes.Abstract | TypeAttributes.Sealed;

        Types.Types.Instance.All.Where(_ =>
            (_.Attributes & staticType) != staticType &&
            !_.IsInterface &&
            !_.IsAbstract &&
            !ShouldIgnoreNamespace(_.Namespace ?? string.Empty) &&
            !HasConstructorWithUnresolvableParameters(_) &&
            !HasConstructorWithRecordTypes(_) &&
            !_.IsAssignableTo(typeof(Exception)) &&
            services.Any(s => s.ServiceType != _)).ToList().ForEach(_ =>
        {
            var __ = _.HasAttribute<SingletonAttribute>() ?
                services.AddSingleton(_, _) :
                services.AddTransient(_, _);
        });

        return services;
    }

    static bool ShouldIgnoreNamespace(string namespaceToCheck) =>
        _namespacesToIgnoreForSelfBinding.Any(namespaceToCheck.StartsWith);

    static bool HasConstructorWithUnresolvableParameters(Type type) =>
        type.GetConstructors().Any(_ => _.GetParameters().Any(p => p.ParameterType.IsAPrimitiveType()));

    static bool HasConstructorWithRecordTypes(Type type) =>
        type.GetConstructors().Any(_ => _.GetParameters().Any(p => p.ParameterType.IsRecord()));
}
