// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
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
        var field = typeof(SerializationConfigurationExtensions).GetField("_converters", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("SerializationConfigurationExtensions._converters could not be reflected.");
        var converters = (IEnumerable<JsonConverter>)field.GetValue(null)!;
        _hasCustomConverter = type => converters.Any(_ => _.CanConvert(type));
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
