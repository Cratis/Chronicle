// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;

namespace Cratis.Chronicle.Tools.ProtoGenerator;

/// <summary>
/// Writes the canonical binary <c>FileDescriptorSet</c> describing every Chronicle wire contract.
/// </summary>
/// <remarks>
/// This is the single artifact every Chronicle client - .NET, Kotlin, TypeScript and Elixir - ships
/// embedded in its contracts package and hands to the server on connect. Only two of those four
/// generators keep descriptors around at runtime, so producing the set once at build time is what
/// lets the server own the one and only compatibility check.
/// </remarks>
public static class DescriptorSetWriter
{
    /// <summary>
    /// The file name of the canonical descriptor set, as it is written next to the proto files and
    /// as it is embedded in every contracts package.
    /// </summary>
    public const string FileName = "chronicle.desc";

    /// <summary>
    /// Writes the descriptor set built from every proto file in a directory.
    /// </summary>
    /// <param name="protoDirectory">Directory holding the generated <c>.proto</c> files.</param>
    /// <returns>The errors reported while parsing, empty when the set was written cleanly.</returns>
    public static Error[] Write(string protoDirectory)
    {
        var (set, errors) = Build(protoDirectory);
        if (Array.Exists(errors, _ => _.IsError))
        {
            return errors;
        }

        using var stream = File.Create(Path.Combine(protoDirectory, FileName));
        ProtoBuf.Serializer.Serialize(stream, set);
        return errors;
    }

    /// <summary>
    /// Builds the descriptor set from every proto file in a directory, without writing it.
    /// </summary>
    /// <param name="protoDirectory">Directory holding the <c>.proto</c> files.</param>
    /// <param name="additionalImportPaths">Extra directories to resolve imports from.</param>
    /// <returns>The parsed set and the errors reported while parsing.</returns>
    public static (FileDescriptorSet Set, Error[] Errors) Build(string protoDirectory, params string[] additionalImportPaths)
    {
        var set = new FileDescriptorSet();
        set.AddImportPath(protoDirectory);
        Array.ForEach(additionalImportPaths, set.AddImportPath);

        // Imported protos live in subdirectories (protobuf-net/bcl.proto, google/protobuf/*.proto) and are pulled
        // in by the import statements of the files added below, so only the top-level files are added explicitly -
        // each of those is one Chronicle contracts package.
        foreach (var file in Directory.GetFiles(protoDirectory, "*.proto").Order(StringComparer.Ordinal))
        {
            set.Add(Path.GetFileName(file), includeInOutput: true);
        }

        set.Process();
        return (set, set.GetErrors());
    }
}
