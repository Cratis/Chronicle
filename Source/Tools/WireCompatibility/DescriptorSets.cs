// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.ProtoGenerator;
using Google.Protobuf.Reflection;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// Produces the descriptor set for a version of the Chronicle contracts.
/// </summary>
public static class DescriptorSets
{
    /// <summary>
    /// Reads a descriptor set that has already been generated.
    /// </summary>
    /// <param name="path">The path to a serialized <c>FileDescriptorSet</c>.</param>
    /// <returns>The parsed set.</returns>
    public static FileDescriptorSet ReadFrom(string path)
    {
        using var stream = File.OpenRead(path);
        return ProtoBuf.Serializer.Deserialize<FileDescriptorSet>(stream);
    }

    /// <summary>
    /// Generates the descriptor set for a contracts assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the contracts assembly.</param>
    /// <param name="importPath">A directory to resolve proto imports that the generated schemas depend on from.</param>
    /// <returns>The generated set.</returns>
    /// <exception cref="CouldNotBuildDescriptorSet">Thrown when the generated schemas do not parse.</exception>
    /// <remarks>
    /// Released contracts packages from before the descriptor set became a build artifact do not carry one, and the
    /// baseline for the current major is one of those - so the set is generated from the assembly, through the same
    /// generator that produced the current one.
    /// </remarks>
    public static FileDescriptorSet GenerateFor(string assemblyPath, string importPath)
    {
        var directory = Directory.CreateTempSubdirectory("chronicle-wire-schema-").FullName;

        var loadContext = new BaselineLoadContext();

        try
        {
            ProtoSchemaGeneration.Write(ProtoSchemaGeneration.Generate(loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath))), directory);

            var (set, errors) = DescriptorSetWriter.Build(directory, importPath);
            var failures = Array.FindAll(errors, _ => _.IsError);

            return failures.Length > 0
                ? throw new CouldNotBuildDescriptorSet(assemblyPath, failures.Select(_ => _.ToString()))
                : set;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
            loadContext.Unload();
        }
    }
}
