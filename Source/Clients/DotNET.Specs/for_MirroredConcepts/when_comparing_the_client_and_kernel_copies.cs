// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reflection;

namespace Cratis.Chronicle.for_MirroredConcepts;

/// <summary>
/// A concept that exists on both sides of the wire exists twice: once in the kernel under
/// <c>Cratis.Chronicle.Concepts</c>, and once in the client under the same namespace without that segment. The two
/// copies are hand-maintained, and nothing in the compiler connects them.
/// </summary>
/// <remarks>
/// <para>
/// The sentinels and enum values in those copies are wire values. When the client's
/// <c>EventSequenceNumber.Max</c> said <c>ulong.MaxValue - 2</c> and the kernel's said <c>ulong.MaxValue - 1</c>, a
/// concurrency scope the client meant as "no expected sequence number" arrived at the kernel as an ordinary number,
/// and the kernel ran a check that could only ever pass. Both sides compiled, every specification was green, and
/// nothing anywhere compared the two.
/// </para>
/// <para>
/// This compares them. Not every same-named pair is a copy - plenty are deliberately different types that happen to
/// share a name, with the client carrying a slimmer consumer-facing shape - so this asserts only on what both sides
/// actually declare. A member present on both must agree; a member on only one is that side's own business.
/// Comparison is by runtime value rather than by source text, because the two copies routinely spell the same value
/// differently (<c>Guid.Empty</c> against <c>new(Guid.Empty)</c>, a literal against a named constant).
/// </para>
/// </remarks>
public class when_comparing_the_client_and_kernel_copies : Specification
{
    const string KernelConceptsNamespace = "Cratis.Chronicle.Concepts";
    const string ClientNamespace = "Cratis.Chronicle";

    IReadOnlyList<(Type Kernel, Type Client)> _pairs;
    List<string> _disagreements;

    void Establish()
    {
        var kernelAssembly = typeof(KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSequenceNumber).Assembly;
        var clientAssembly = typeof(Events.EventSequenceNumber).Assembly;

        var clientTypes = clientAssembly.GetExportedTypes()
            .Where(_ => _.FullName is not null)
            .GroupBy(_ => _.FullName!)
            .ToDictionary(_ => _.Key, _ => _.First());

        _pairs = [.. kernelAssembly.GetExportedTypes()
            .Where(_ => _.Namespace?.StartsWith(KernelConceptsNamespace, StringComparison.Ordinal) == true)
            .Select(kernelType => (Kernel: kernelType, ClientName: ToClientName(kernelType)))
            .Where(_ => clientTypes.ContainsKey(_.ClientName))
            .Select(_ => (_.Kernel, Client: clientTypes[_.ClientName]))];
    }

    void Because()
    {
        _disagreements = [];
        foreach (var (kernel, client) in _pairs)
        {
            _disagreements.AddRange(kernel.IsEnum && client.IsEnum
                ? CompareEnumValues(kernel, client)
                : CompareConstants(kernel, client));
        }
    }

    [Fact] void should_find_the_mirrored_concepts_to_compare() => _pairs.Count.ShouldBeGreaterThan(20);
    [Fact] void should_find_no_disagreement() => string.Join(Environment.NewLine, _disagreements).ShouldEqual(string.Empty);

    static string ToClientName(Type kernelType) =>
        $"{ClientNamespace}{kernelType.Namespace![KernelConceptsNamespace.Length..]}.{kernelType.Name}";

    /// <summary>
    /// Enum members cross the wire as their underlying numbers, so a member both sides declare has to number the
    /// same on both. A member only one side declares is that side's own business.
    /// </summary>
    /// <param name="kernel">The kernel's copy.</param>
    /// <param name="client">The client's copy.</param>
    /// <returns>One description per member the two number differently.</returns>
    static IEnumerable<string> CompareEnumValues(Type kernel, Type client)
    {
        var clientValues = Enum.GetNames(client).Zip(Enum.GetValuesAsUnderlyingType(client).Cast<object>())
            .ToDictionary(_ => _.First, _ => Convert.ToInt64(_.Second, System.Globalization.CultureInfo.InvariantCulture));

        foreach (var (name, value) in Enum.GetNames(kernel).Zip(Enum.GetValuesAsUnderlyingType(kernel).Cast<object>()))
        {
            var kernelValue = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
            if (clientValues.TryGetValue(name, out var found) && found != kernelValue)
            {
                yield return $"{kernel.FullName}.{name} is {kernelValue} in the kernel and {found} in the client.";
            }
        }
    }

    /// <summary>
    /// The sentinels. A public constant or static readonly field declared on both copies has to carry the same
    /// value, whichever way each side spells it.
    /// </summary>
    /// <param name="kernel">The kernel's copy.</param>
    /// <param name="client">The client's copy.</param>
    /// <returns>One description per constant the two disagree on.</returns>
    static IEnumerable<string> CompareConstants(Type kernel, Type client)
    {
        var clientFields = PublicValues(client);
        foreach (var (name, kernelValue) in PublicValues(kernel))
        {
            if (!clientFields.TryGetValue(name, out var clientValue))
            {
                continue;
            }

            if (!Equals(kernelValue, clientValue))
            {
                yield return $"{kernel.FullName}.{name} is '{kernelValue}' in the kernel and '{clientValue}' in the client.";
            }
        }
    }

    /// <summary>
    /// The public constants and static readonly fields of a type, reduced to values that can be compared across
    /// the two assemblies.
    /// </summary>
    /// <param name="type">Type to read.</param>
    /// <returns>The comparable values, by name.</returns>
    static Dictionary<string, object?> PublicValues(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(_ => _.IsLiteral || _.IsInitOnly)
            .Select(_ => (_.Name, Value: Underlying(SafeGet(_))))
            .Where(_ => _.Value is not null)
            .GroupBy(_ => _.Name)
            .ToDictionary(_ => _.Key, _ => _.First().Value);

    /// <summary>
    /// Read a static field, tolerating one whose initializer throws.
    /// </summary>
    /// <param name="field">Field to read.</param>
    /// <returns>The value, or null when it cannot be read.</returns>
    static object? SafeGet(FieldInfo field)
    {
        try
        {
            return field.GetValue(null);
        }
        catch (TargetInvocationException)
        {
            // A sentinel whose initializer throws is not a value this can compare, and is not what this is for.
            return null;
        }
    }

    /// <summary>
    /// The two copies are different CLR types, so a concept has to be compared by what it wraps rather than by
    /// itself. Anything that is not a concept or a primitive is left alone - this is about values, not structure.
    /// </summary>
    /// <param name="value">Value to reduce.</param>
    /// <returns>The comparable value, or null when there is not one.</returns>
    static object? Underlying(object? value) => value switch
    {
        null => null,
        string or bool => value,
        _ when value.GetType().IsPrimitive => value,
        _ when value.GetType().IsConcept() => value.GetConceptValue(),
        _ => null
    };
}
