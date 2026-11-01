// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Usage: RollingDeliveryIl normal.dll [untouched-baseline.dll] [--enabled diagnostic.dll]
// Reads metadata/IL only; never loads or executes the inspected assemblies.
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

if (args.Length == 0)
{
    throw new ArgumentException("Expected normal assembly path, optional baseline, and optional --enabled assembly.");
}

using var normalStream = File.OpenRead(args[0]);
using var normal = new PEReader(normalStream);
AssertDiagnostics(normal, false);
var index = 1;
if (index < args.Length && args[index] != "--enabled")
{
    using var baselineStream = File.OpenRead(args[index++]);
    using var baseline = new PEReader(baselineStream);
    AssertEquivalent(baseline, normal);
}
if (index < args.Length)
{
    if (args[index++] != "--enabled" || index != args.Length - 1)
    {
        throw new ArgumentException("Expected --enabled followed by one diagnostic assembly path.");
    }
    using var enabledStream = File.OpenRead(args[index]);
    using var enabled = new PEReader(enabledStream);
    AssertDiagnostics(enabled, true);
}

static void AssertEquivalent(PEReader baseline, PEReader normal)
{
    var expected = DescribeTouchedCode(baseline);
    var actual = DescribeTouchedCode(normal);
    if (!expected.Keys.SequenceEqual(actual.Keys))
    {
        throw new InvalidOperationException("Touched no-symbol type/method/field sets differ from the untouched baseline.");
    }
    foreach (var entry in expected)
    {
        if (actual[entry.Key] != entry.Value)
        {
            throw new InvalidOperationException($"No-symbol IL/layout differs: {entry.Key}");
        }
    }
    Console.WriteLine($"Identical no-symbol IL/layout: {actual.Count} touched method/field records.");
}

static void AssertDiagnostics(PEReader assembly, bool expected)
{
    var metadata = assembly.GetMetadataReader();
    var definitions = metadata.TypeDefinitions.Select(handle => metadata.GetString(metadata.GetTypeDefinition(handle).Name));
    var references = metadata.TypeReferences.Select(handle => metadata.GetString(metadata.GetTypeReference(handle).Name));
    var found = definitions.Concat(references).Any(IsDiagnosticName);
    if (found != expected)
    {
        throw new InvalidOperationException($"Diagnostic type/reference presence was not {expected}.");
    }
    var code = DescribeTouchedCode(assembly);
    Console.WriteLine($"Diagnostic code {(expected ? "present" : "absent")}: {code.Count} touched method/field records inspected.");
}

static bool IsDiagnosticName(string name) => name.Contains("ReactorsDeliveryDiagnostics", StringComparison.Ordinal);

static bool IsTouchedRoot(string name) => name is "Cratis.Chronicle.Reactors.Reactors" or "Cratis.Chronicle.Reactors.ReactorDispatcher";

static SortedDictionary<string, string> DescribeTouchedCode(PEReader assembly)
{
    var metadata = assembly.GetMetadataReader();
    var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
    var roots = new HashSet<string>(StringComparer.Ordinal);
    foreach (var handle in metadata.TypeDefinitions)
    {
        var name = FullName(metadata, handle);
        var root = name.Split('+')[0];
        if (!IsTouchedRoot(root))
        {
            continue;
        }
        roots.Add(root);
        DescribeType(assembly, handle, name, result);
    }
    if (roots.Count != 2 || result.Count == 0)
    {
        throw new InvalidOperationException("Both touched root types and their code must be present.");
    }
    return result;
}

static void DescribeType(PEReader assembly, TypeDefinitionHandle handle, string name, SortedDictionary<string, string> result)
{
    var metadata = assembly.GetMetadataReader();
    var type = metadata.GetTypeDefinition(handle);
    result.Add($"{name}/type", $"{type.Attributes}:{MetadataTokens.GetToken(type.BaseType)}");
    foreach (var fieldHandle in type.GetFields())
    {
        var field = metadata.GetFieldDefinition(fieldHandle);
        result.Add($"{name}/field/{metadata.GetString(field.Name)}", $"{field.Attributes}:{Convert.ToHexString(metadata.GetBlobBytes(field.Signature))}");
    }
    foreach (var methodHandle in type.GetMethods())
    {
        var method = metadata.GetMethodDefinition(methodHandle);
        var key = $"{name}/{metadata.GetString(method.Name)}/{Convert.ToHexString(metadata.GetBlobBytes(method.Signature))}";
        result.Add(key, $"{method.Attributes}:{method.ImplAttributes}:{DescribeBody(assembly, method)}");
    }
}

static string DescribeBody(PEReader assembly, MethodDefinition method)
{
    if (method.RelativeVirtualAddress == 0)
    {
        return "no body";
    }
    var metadata = assembly.GetMetadataReader();
    var body = assembly.GetMethodBody(method.RelativeVirtualAddress);
    var locals = body.LocalSignature.IsNil ? "" : Convert.ToHexString(metadata.GetBlobBytes(metadata.GetStandaloneSignature(body.LocalSignature).Signature));
    var handlers = string.Join(";", body.ExceptionRegions.Select(DescribeRegion));
    return $"{body.MaxStack}:{body.LocalVariablesInitialized}:{locals}:{handlers}:{Convert.ToHexString(body.GetILBytes()!)}";
}

static string DescribeRegion(ExceptionRegion region) =>
    $"{region.Kind}:{region.TryOffset}:{region.TryLength}:{region.HandlerOffset}:{region.HandlerLength}:{region.FilterOffset}:{MetadataTokens.GetToken(region.CatchType)}";

static string FullName(MetadataReader metadata, TypeDefinitionHandle handle)
{
    var type = metadata.GetTypeDefinition(handle);
    var parent = type.GetDeclaringType();
    return parent.IsNil
        ? $"{metadata.GetString(type.Namespace)}.{metadata.GetString(type.Name)}"
        : $"{FullName(metadata, parent)}+{metadata.GetString(type.Name)}";
}
