// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers;

public class when_round_tripping_every_grain_payload : given.the_kernel_serializers
{
    /// <summary>
    /// AppendedEvent.ErasedProperties is derived data that has never been part of what travels between grains.
    /// </summary>
    static readonly string[] _membersThatDoNotTravel = ["ErasedProperties"];
    given.PayloadComparer _comparer;
    IReadOnlyList<Type> _payloads;
    IReadOnlyList<string> _unresolved;
    List<string> _lostBetweenSilos;
    List<string> _lostWithinSilo;
    List<string> _notChecked;

    void Because()
    {
        _comparer = new given.PayloadComparer(_membersThatDoNotTravel);
        (_payloads, _unresolved) = given.PayloadManifest.Read();
        _lostBetweenSilos = [];
        _lostWithinSilo = [];
        _notChecked = [];

        foreach (var type in _payloads)
        {
            object? original;
            try
            {
                original = new given.PayloadFiller().Create(type);
            }
            catch (TargetInvocationException ex)
            {
                _notChecked.Add($"{type.FullName}: {ex.InnerException?.GetType().Name} {ex.InnerException?.Message} {string.Join(" | ", (ex.InnerException?.StackTrace ?? "").Split((char)10).Take(4))}");
                continue;
            }

            if (original is null || (original.GetType() != type && !type.IsAbstract && !type.IsInterface))
            {
                _notChecked.Add(type.FullName!);
                continue;
            }

            Check(_lostBetweenSilos, type, original, SendBetweenSilos);
            Check(_lostWithinSilo, type, original, PassWithinSilo);
        }
    }

    [Fact] void should_resolve_every_payload_in_the_manifest() => _unresolved.ShouldBeEmpty();
    [Fact] void should_check_every_payload() => _notChecked.ShouldBeEmpty();
    [Fact] void should_cover_the_payloads() => _payloads.Count.ShouldBeGreaterThan(100);
    [Fact] void should_keep_every_member_between_silos() => _lostBetweenSilos.ShouldBeEmpty();
    [Fact] void should_keep_every_member_within_a_silo() => _lostWithinSilo.ShouldBeEmpty();

    void Check(List<string> failures, Type type, object original, Func<Type, object, object?> transport)
    {
        try
        {
            var result = transport(type, original);
            var differences = _comparer.Differences(original, result);
            if (differences.Count > 0)
            {
                failures.Add($"{type.FullName}: {string.Join("; ", differences.Take(3))}");
            }
        }
        catch (Exception ex)
        {
            var cause = ex is TargetInvocationException { InnerException: not null } wrapped ? wrapped.InnerException : ex;
            failures.Add($"{type.FullName}: {cause.GetType().Name} {cause.Message.Split('\n')[0]} {string.Join(" | ", (cause.StackTrace ?? "").Split((char)10).Take(4))}");
        }
    }
}
