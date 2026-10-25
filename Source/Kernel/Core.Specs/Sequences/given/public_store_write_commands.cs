// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventStores;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;

namespace Cratis.Chronicle.Sequences.given;

public static class public_store_write_commands
{
    static readonly Type[] _types = typeof(Append).Assembly.GetTypes();

    public static IEnumerable<Type> AllTypes => _types
        .Where(type => type.IsDefined(typeof(CommandAttribute), false))
        .Concat(_types.Select(ValidatedCommand).OfType<Type>())
        .Distinct()
        .OrderBy(type => type.FullName);

    public static IEnumerable<Type> Types => AllTypes.Where(type => system_store_command_policy.Guarded.Contains(type.FullName!));

    public static IEnumerable<Type> AllowedStoreTypes => AllTypes.Where(type =>
        system_store_command_policy.Allowed.ContainsKey(type.FullName!) && type.GetProperty("EventStore") is not null);

    public static async Task<ValidationResult> ValidateStore(Type commandType, string storeName)
    {
        var store = StoreProperty(commandType);
        var validator = CreateValidator(commandType);
        if (validator is null) return new ValidationResult();
        var command = CreateCommand(commandType, store, storeName);
        var memberNames = new[] { store.Name, char.ToLowerInvariant(store.Name[0]) + store.Name[1..] };
        var context = new ValidationContext<object>(command, new PropertyChain(), new MemberNameValidatorSelector(memberNames));
        return await validator.ValidateAsync(context);
    }

    public static string RefusalMessage(Type commandType) => commandType == typeof(EnsureEventStore)
        ? "The System event store is reserved for the kernel and cannot be created through the public API."
        : "The System event store is reserved for the kernel and cannot be written through the public API.";

    public static string StoreMember(Type commandType) => StoreProperty(commandType).Name;

    static Type? ValidatedCommand(Type type) =>
        type.BaseType is { IsGenericType: true } parent && parent.GetGenericTypeDefinition() == typeof(CommandValidator<>)
            ? parent.GenericTypeArguments[0] : null;

    static PropertyInfo StoreProperty(Type type) => type.GetProperty(type == typeof(EnsureEventStore) ? "Name" : "EventStore")!;

    static IValidator? CreateValidator(Type commandType)
    {
        var validatorType = _types.SingleOrDefault(type => ValidatedCommand(type) == commandType);
        if (validatorType is null)
        {
            if (system_store_command_policy.Allowed.ContainsKey(commandType.FullName!)) return null;
            throw new InvalidOperationException($"A System-store write validator is required for {commandType.FullName}.");
        }
        var constructor = validatorType.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).First();

        // Only store rules run; concrete collaborators such as WebhookRegistrar must never be invoked.
        var arguments = constructor.GetParameters()
            .Select(parameter => parameter.ParameterType.IsInterface ? Substitute.For([parameter.ParameterType], []) : null)
            .ToArray();
        return (IValidator)constructor.Invoke(arguments);
    }

    static object CreateCommand(Type commandType, PropertyInfo store, string storeName)
    {
        var constructor = commandType.GetConstructors().Single(candidate => candidate.GetParameters()
            .Any(parameter => string.Equals(parameter.Name, store.Name, StringComparison.OrdinalIgnoreCase)));
        var arguments = constructor.GetParameters().Select(parameter =>
            string.Equals(parameter.Name, store.Name, StringComparison.OrdinalIgnoreCase)
                ? StoreValue(parameter.ParameterType, storeName)
                : DefaultValue(parameter)).ToArray();
        return constructor.Invoke(arguments);
    }

    static object StoreValue(Type type, string value)
    {
        if (type == typeof(EventStoreName))
        {
            return new EventStoreName(value);
        }

        return value;
    }

    static object? DefaultValue(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
    }
}
