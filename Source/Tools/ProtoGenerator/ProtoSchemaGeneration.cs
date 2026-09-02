// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Reflection;

namespace Cratis.Chronicle.Tools.ProtoGenerator;

/// <summary>
/// Generates the proto schema for every contracts package in an assembly.
/// </summary>
/// <remarks>
/// This is shared with the wire-compatibility tool, which generates the schema for a previously released contracts
/// assembly it downloaded from NuGet. Both sides of that comparison have to be produced the same way, or a
/// difference in how they were generated reads as a difference in the contract.
/// </remarks>
public static class ProtoSchemaGeneration
{
    /// <summary>
    /// Generates the proto schema for every package in a contracts assembly.
    /// </summary>
    /// <param name="assembly">The contracts assembly to read.</param>
    /// <returns>The schema for each package, keyed by the proto file name it belongs in.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a package cannot be generated, or when a type that declares retired field numbers gets no
    /// reservation in any package - a reservation that quietly does not happen frees the number for reuse again.
    /// </exception>
    public static IReadOnlyDictionary<string, string> Generate(Assembly assembly)
    {
        var contractTypes = assembly.ExportedTypes.ToArray();
        var services = Array.FindAll(contractTypes, _ => _.IsInterface && IsService(_));

        // Reservations are declared on types across the whole contracts assembly while the schema is generated one
        // package at a time, so a package is only handed the reserved types it actually has a message for. Passing
        // all of them to every package made generation throw for every package but the one owning the type. See
        // Cratis/Chronicle#3712.
        var typesWithReservations = ProtoSchemaHelper.WithReservedFields(contractTypes);
        var reservationsApplied = new HashSet<Type>();
        var generator = new SchemaGenerator();
        var schemas = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var group in services.GroupBy(_ => _.Namespace))
        {
            var packageName = group.Key ?? "default";
            string schema;

            try
            {
                schema = generator.GetSchema([.. group]);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate the proto schema for package '{packageName}'.", ex);
            }

            // In proto3 an rpc whose name matches its input message type is a scoping ambiguity.
            schema = ProtoSchemaHelper.FixRpcMethodNameConflicts(schema);

            // Enum values use C++ scoping rules and have to be unique within the package.
            schema = ProtoSchemaHelper.FixEnumValueConflicts(schema);

            schema = ProtoSchemaHelper.AddSerializableDateTimeOffsetComment(schema);

            var reservationsForPackage = typesWithReservations.Where(_ => ProtoSchemaHelper.DeclaresMessage(schema, _.Name)).ToArray();
            schema = ProtoSchemaHelper.DeclareReservedFields(schema, reservationsForPackage);
            reservationsApplied.UnionWith(reservationsForPackage);

            schemas[FileNameFor(packageName)] = schema;
        }

        var reservationsLost = typesWithReservations.Except(reservationsApplied).ToArray();
        if (reservationsLost.Length > 0)
        {
            throw new InvalidOperationException(
                $"No generated package declares a message for {string.Join(", ", reservationsLost.Select(_ => $"'{_.FullName}'"))}, so their retired field numbers were not reserved.");
        }

        return schemas;
    }

    /// <summary>
    /// Writes generated schemas out as proto files, removing whatever was there before.
    /// </summary>
    /// <param name="schemas">The schemas, keyed by proto file name.</param>
    /// <param name="outputDirectory">The directory to write them into.</param>
    /// <returns>The paths written.</returns>
    /// <remarks>
    /// The previous contents are deleted rather than overwritten, so a file that stops being generated - a package
    /// that was renamed or removed - cannot linger and keep describing a contract that no longer exists. It also
    /// makes hand-patching a generated file impossible to keep rather than merely discouraged: three fields once
    /// lived in eventsequences.proto that nothing in the contracts produced, and nothing noticed for months.
    /// <para>
    /// Deleting happens only once every schema has been generated, so a generator failure leaves the committed
    /// files untouched rather than emptying the directory.
    /// </para>
    /// Files in subdirectories are left alone - <c>protobuf-net/bcl.proto</c> is protobuf-net's own file, an input
    /// to generation rather than an output of it.
    /// </remarks>
    public static IReadOnlyList<string> Write(IReadOnlyDictionary<string, string> schemas, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (var stale in Directory.GetFiles(outputDirectory, "*.proto").Concat(Directory.GetFiles(outputDirectory, "*.desc")))
        {
            File.Delete(stale);
        }

        return
        [
            .. schemas.Select(_ =>
            {
                var path = Path.Combine(outputDirectory, $"{_.Key}.proto");
                File.WriteAllText(path, _.Value);
                return path;
            })
        ];
    }

    /// <summary>
    /// Determines whether a type is a gRPC service contract.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when it carries the service attribute, false otherwise.</returns>
    /// <remarks>
    /// The contracts assembly being read is loaded on its own, so its <c>ServiceAttribute</c> is not necessarily the
    /// same type as this tool's - a released assembly can carry a different protobuf-net.Grpc. Match by name.
    /// </remarks>
    static bool IsService(Type type) =>
        Array.Exists(type.GetCustomAttributes(inherit: false), _ => string.Equals(_.GetType().Name, nameof(ServiceAttribute), StringComparison.Ordinal));

    static string FileNameFor(string packageName)
    {
        var fileName = packageName
            .Replace("Cratis.Chronicle.Contracts.", string.Empty, StringComparison.Ordinal)
            .Replace('.', '_')
            .ToLowerInvariant();

        return string.IsNullOrEmpty(fileName) ? "chronicle" : fileName;
    }
}
