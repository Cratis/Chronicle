// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_MongoDBReservedEventSequenceIds.given;

/// <summary>
/// Reads the collection names the storage provider opens.
/// </summary>
/// <remarks>
/// Bare identifiers are dynamic names from event sequences, read models or sinks and are not catalog entries.
/// </remarks>
internal static partial class StorageSource
{
    const string CollectionCall = @"GetCollection\s*<[^;()]+?>\s*\(\s*";
    static readonly HashSet<string> _excludedDirectories = new(StringComparer.Ordinal) { "bin", "obj", "Migrations" };
    static readonly HashSet<string> _wrappers = new(StringComparer.Ordinal)
    {
        "EventStoreNamespaceDatabase.cs", "EventStoreDatabase.cs", "Database.cs"
    };

    /// <summary>
    /// Finds explicitly named collections, derived suffixes and type-derived calls.
    /// </summary>
    /// <returns>The names and calls read from the storage sources.</returns>
    internal static (HashSet<string> Names, HashSet<string> Tails, HashSet<string> TypeDerivedCalls) Scan()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var tails = new HashSet<string>(StringComparer.Ordinal);
        var typeDerivedCalls = new HashSet<string>(StringComparer.Ordinal);
        var types = typeof(WellKnownCollectionNames).Assembly.GetTypes();
        foreach (var path in SourceFiles())
        {
            var source = File.ReadAllText(path);
            names.UnionWith(Literal.Matches(source).Concat(Constant.Matches(source)).Select(match => match.Groups["name"].Value));
            names.UnionWith(Member.Matches(source).Select(match => Resolve(types, match)).OfType<string>());
            tails.UnionWith(Derived.Matches(source).Select(match => match.Groups["tail"].Value));
            if (!_wrappers.Contains(Path.GetFileName(path)) && Empty.IsMatch(source))
            {
                typeDerivedCalls.Add(path);
            }
        }

        return (names, tails, typeDerivedCalls);
    }

    static string? Resolve(Type[] types, Match match) => types
        .Where(type => type.Name == match.Groups["type"].Value)
        .Select(type => type.GetField(match.Groups["member"].Value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        .OfType<FieldInfo>()
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string?)field.GetRawConstantValue())
        .FirstOrDefault();

    static IEnumerable<string> SourceFiles()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var root = Path.Combine(directory.FullName, "Source", "Kernel", "Storage.MongoDB");
            if (File.Exists(Path.Combine(root, "Storage.MongoDB.csproj")))
            {
                return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar)
                        .Any(_excludedDirectories.Contains));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Source/Kernel/Storage.MongoDB/Storage.MongoDB.csproj for the storage catalog scan.");
    }

    [GeneratedRegex(CollectionCall + "\"(?<name>[^\"]+)\"", RegexOptions.Compiled, 1000)]
    private static partial Regex Literal { get; }

    [GeneratedRegex(CollectionCall + @"(?<type>\w+)\.(?<member>\w+)", RegexOptions.Compiled, 1000)]
    private static partial Regex Member { get; }

    [GeneratedRegex("const\\s+string\\s+\\w*CollectionName\\s*=\\s*\"(?<name>[^\"]+)\"", RegexOptions.Compiled, 1000)]
    private static partial Regex Constant { get; }

    [GeneratedRegex(CollectionCall + "\\$\"\\{[^}]+\\}(?<tail>[^\"]+)\"", RegexOptions.Compiled, 1000)]
    private static partial Regex Derived { get; }

    [GeneratedRegex(CollectionCall + @"\)", RegexOptions.Compiled, 1000)]
    private static partial Regex Empty { get; }
}
