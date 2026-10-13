// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Cratis.Chronicle.Events;
using static Cratis.Chronicle.Reducers.ReducerFingerprint;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Fingerprints registry-dependent static dispatch and selected implementations, including inherited bodies.
/// </summary>
internal static class SelfReducerFingerprint
{
    const BindingFlags DeclaredMethods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Creates a fingerprint for the owning registry's selected static transitions and hooks.
    /// </summary>
    /// <param name="eventTypes">The owning event registry.</param>
    /// <param name="model">The self-reducing model.</param>
    /// <returns>The stable fingerprint.</returns>
    internal static string Create(IEventTypes eventTypes, Type model)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, ReducerFingerprint.Create(model));
        var dispatch = SelfReducerMethods.Get(eventTypes, model);
        foreach (var (type, method) in dispatch.OrderBy(pair => GetTypeIdentity(pair.Key), StringComparer.Ordinal))
        {
            var identity = eventTypes.GetEventTypeFor(type);
            Append(hash, GetTypeIdentity(type));
            Append(hash, identity.Id.Value);
            Append(hash, identity.Generation.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(hash, GetMemberIdentity(method));
        }
        foreach (var method in SelectedMethods(eventTypes, model))
        {
            AppendImplementation(hash, method);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    /// <summary>
    /// Creates a conservative fallback spanning the model and every selected declaring module.
    /// </summary>
    /// <param name="eventTypes">The owning registry.</param>
    /// <param name="model">The self model.</param>
    /// <returns>The conservative fingerprint.</returns>
    internal static string CreateFallback(IEventTypes eventTypes, Type model)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, ReducerFingerprint.CreateFallback(model));
        foreach (var (type, method) in SelfReducerMethods.Get(eventTypes, model).OrderBy(pair => GetTypeIdentity(pair.Key), StringComparer.Ordinal))
        {
            var identity = eventTypes.GetEventTypeFor(type);
            Append(hash, GetTypeIdentity(type));
            Append(hash, identity.Id.Value);
            Append(hash, identity.Generation.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(hash, GetMemberIdentity(method));
        }
        foreach (var method in SelectedMethods(eventTypes, model))
        {
            Append(hash, GetMemberIdentity(method));
            Append(hash, method.Module.ModuleVersionId.ToString("D"));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    static IEnumerable<MethodInfo> SelectedMethods(IEventTypes eventTypes, Type model) =>
        SelfReducerMethods.Get(eventTypes, model).Values
            .Concat(new SelfReducerReplayHooks(eventTypes, model).Methods)
            .Distinct().OrderBy(GetMemberIdentity, StringComparer.Ordinal).ThenBy(method => method.Module.ScopeName, StringComparer.Ordinal);

    static void AppendImplementation(IncrementalHash hash, MethodInfo method)
    {
        // MethodInfo identity is module-aware; never deduplicate selected bodies by type/member display names.
        Append(hash, GetMemberIdentity(method));
        Append(hash, method.Module.ScopeName);
        ReducerILFingerprint.AppendBody(hash, method);
        if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } stateMachine)
        {
            foreach (var step in stateMachine.StateMachineType.GetMethods(DeclaredMethods).OrderBy(GetMemberIdentity, StringComparer.Ordinal))
            {
                Append(hash, GetMemberIdentity(step));
                ReducerILFingerprint.AppendBody(hash, step);
            }
        }
    }
}
