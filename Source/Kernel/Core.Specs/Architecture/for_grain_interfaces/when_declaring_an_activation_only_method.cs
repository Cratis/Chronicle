// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Architecture.for_grain_interfaces;

/// <summary>
/// A parameterless Ensure() exists to force activation. Where it does no work there is nothing for
/// non-reentrancy to protect and everything to lose by queueing it behind real work: it has been
/// missed twice, on IProjectionsManager (#3848) and IObserver (#3852), and both times it took a silo
/// down during startup rather than failing anywhere near the cause. Checking it costs a test.
/// </summary>
public class when_declaring_an_activation_only_method : Specification
{
    /// <summary>
    /// Grains whose Ensure() genuinely does work, and so must keep its turn. Removing an entry is only
    /// correct once that Ensure() no longer touches state or storage.
    /// <list type="bullet">
    /// <item><c language="csharp">ICapturesManager.Ensure</c> reads every capture and starts the ones marked started.</item>
    /// </list>
    /// </summary>
    static readonly string[] _doesRealWorkAndMustNotInterleave = ["ICapturesManager.Ensure"];

    IEnumerable<Type> _grainInterfaces;
    IEnumerable<string> _activationOnlyMethods;
    IEnumerable<string> _withoutAlwaysInterleave;

    void Establish() => _grainInterfaces = typeof(IObserver).Assembly
        .GetTypes()
        .Where(type => type.IsInterface && typeof(IAddressable).IsAssignableFrom(type));

    void Because()
    {
        var candidates = _grainInterfaces
            .SelectMany(type => type.GetMethods())
            .Where(method => method.Name == "Ensure" && method.GetParameters().Length == 0)
            .Select(method => new { Method = method, Name = $"{method.DeclaringType!.Name}.{method.Name}" })
            .Where(candidate => !_doesRealWorkAndMustNotInterleave.Contains(candidate.Name));

        _activationOnlyMethods = [.. candidates.Select(candidate => candidate.Name)];
        _withoutAlwaysInterleave =
        [
            .. candidates
                .Where(candidate => !Attribute.IsDefined(candidate.Method, typeof(AlwaysInterleaveAttribute)))
                .Select(candidate => candidate.Name)
        ];
    }

    [Fact] void should_always_interleave_it() => _withoutAlwaysInterleave.ShouldBeEmpty();
    [Fact] void should_have_found_some_to_check() => _activationOnlyMethods.ShouldNotBeEmpty();
}
