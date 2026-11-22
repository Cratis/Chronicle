// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given;

public class the_kernel_inventory : Specification
{
    protected ContractManifest _grainContracts;
    protected Type[] _interfaces;
    protected PayloadContractManifest _manifest;
    Func<Type, bool> _hasCustomConverter;

    void Establish()
    {
        _grainContracts = ContractManifest.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "OrleansContracts.txt")));
        var assembly = typeof(SerializationConfigurationExtensions).Assembly;
        _interfaces = _grainContracts.ActiveInterfaces.Select(_ => assembly.GetType(_.InterfaceType, throwOnError: true)!).ToArray();
        var surrogated = assembly.GetTypes()
            .Where(_ => _.GetCustomAttributes().Any(attribute => attribute.GetType().Name == "RegisterConverterAttribute"))
            .SelectMany(_ => _.GetInterfaces())
            .Where(_ => _.IsGenericType && _.GetGenericTypeDefinition().Name == "IConverter`2")
            .Select(_ => _.GetGenericArguments()[0]);

        // Types the kernel writes with a surrogate or a serializer of its own, which the inventory records as opaque.
        var concepts = Assembly.Load("Cratis.Chronicle.Concepts");
        var serialized = new[] { "Events.AppendedEvent", "EventSequences.Concurrency.ConcurrencyScopes", "EventSequences.Concurrency.ConcurrencyScopeConjunctions" }
            .Select(_ => concepts.GetType($"Cratis.Chronicle.Concepts.{_}", throwOnError: true)!);
        var custom = surrogated.Concat(serialized).ToHashSet();
        _hasCustomConverter = custom.Contains;
        _manifest = PayloadInventory.From(_interfaces, _hasCustomConverter);
    }

    protected PayloadContractManifest GenerateAgain() => PayloadInventory.From(_interfaces.Reverse(), _hasCustomConverter);

    protected PayloadTypeContract FirstWithMembers(PayloadKind kind, int minimum = 1) =>
        _manifest.Types.Values.OrderBy(_ => _.Identity, StringComparer.Ordinal).First(_ => _.Kind == kind && _.Members.Count >= minimum);

    protected IReadOnlyList<ContractBreak> CompareChanged(PayloadTypeContract original, PayloadTypeContract changed)
    {
        var types = _manifest.Types.ToDictionary(_ => _.Key, _ => _.Value, StringComparer.Ordinal);
        types.Remove(original.Identity);
        types.Add(changed.Identity, changed);
        return PayloadContractComparison.Compare(_manifest, new(types));
    }
}
