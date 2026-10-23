// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;
using Google.Protobuf.Reflection;

namespace Cratis.Chronicle.Tools.WireCompatibility;

static class SelfTest
{
    public static IReadOnlyList<WireIncompatibilityKind> Cases { get; } =
    [
        WireIncompatibilityKind.FieldRemoved,
        WireIncompatibilityKind.MethodRemoved,
        WireIncompatibilityKind.FieldTypeChanged,
        WireIncompatibilityKind.MethodStreamingChanged,
        WireIncompatibilityKind.EnumValueRemoved
    ];

    public static int Run(string path, TextWriter output)
    {
        var failures = 0;
        var unchanged = WireContractReader.Read(DescriptorSets.ReadFrom(path));
        if (unchanged.Services.Count == 0 || unchanged.Messages.Count == 0 || unchanged.Enums.Count == 0)
        {
            throw new InvalidArguments("The self-test needs Chronicle services, messages and enums in --current.");
        }

        foreach (var kind in Cases)
        {
            var result = Check(path, kind);
            var matches = result.Incompatibilities.Count == 1 && result.Incompatibilities[0].Kind == kind;
            output.WriteLine($"{(matches ? "ok  " : "FAIL")} {kind}: found [{string.Join(", ", result.Incompatibilities.Select(_ => _.Kind))}]");
            failures += matches ? 0 : 1;
        }

        output.WriteLine($"{Cases.Count - failures} of {Cases.Count} planted wire breaks detected as expected.");
        return failures;
    }

    public static WireCompatibilityReport Check(string path, WireIncompatibilityKind kind)
    {
        var baseline = WireContractReader.Read(DescriptorSets.ReadFrom(path));
        var current = DescriptorSets.ReadFrom(path);
        Plant(current, kind);
        return WireCompatibilityChecker.Check(baseline, WireContractReader.Read(current));
    }

    static void Plant(FileDescriptorSet descriptor, WireIncompatibilityKind kind)
    {
        // Never plant in imported Google/protobuf-net support types: these must exercise Chronicle's own schema.
        var files = descriptor.Files.Where(_ => _.Package.StartsWith("Cratis.Chronicle.Contracts", StringComparison.Ordinal)).ToArray();
        switch (kind)
        {
            case WireIncompatibilityKind.FieldRemoved:
                files.SelectMany(_ => _.MessageTypes).First(_ => _.Fields.Count > 0).Fields.RemoveAt(0);
                break;
            case WireIncompatibilityKind.MethodRemoved:
                files.SelectMany(_ => _.Services).First(_ => _.Methods.Count > 0).Methods.RemoveAt(0);
                break;
            case WireIncompatibilityKind.FieldTypeChanged:
                var field = files.SelectMany(_ => _.MessageTypes).SelectMany(_ => _.Fields).First(_ => _.type != FieldDescriptorProto.Type.TypeString);
                field.type = FieldDescriptorProto.Type.TypeString;
                field.TypeName = string.Empty;
                break;
            case WireIncompatibilityKind.MethodStreamingChanged:
                files.SelectMany(_ => _.Services).SelectMany(_ => _.Methods).First(_ => !_.ClientStreaming && !_.ServerStreaming).ServerStreaming = true;
                break;
            case WireIncompatibilityKind.EnumValueRemoved:
                files.SelectMany(_ => _.EnumTypes).First(_ => _.Values.Count > 1).Values.RemoveAt(1);
                break;
            default:
                throw new InvalidArguments($"No planted wire break for {kind}.");
        }
    }
}
