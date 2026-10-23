// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// The grain contracts of one build, read from the OrleansContracts.txt the Orleans contract analyzer maintains.
/// </summary>
/// <param name="Interfaces">The grain interfaces, by the grain interface type Orleans identifies them by.</param>
/// <param name="Classes">The grain classes, by the grain type Orleans identifies them by.</param>
public partial record ContractManifest(
    IReadOnlyDictionary<string, GrainInterfaceContract> Interfaces,
    IReadOnlyDictionary<string, GrainClassContract> Classes)
{
    const string RetiredPrefix = "*RETIRED* ";
    const string MethodIndent = "  ";
    const int MatchTimeoutMilliseconds = 1000;

    /// <summary>
    /// Gets the active interfaces, leaving out the retired ones kept as history.
    /// </summary>
    public IEnumerable<GrainInterfaceContract> ActiveInterfaces => Interfaces.Values.Where(_ => !_.Retired);

    /// <summary>
    /// Gets the active grain classes, leaving out the retired ones kept as history.
    /// </summary>
    public IEnumerable<GrainClassContract> ActiveClasses => Classes.Values.Where(_ => !_.Retired);

    [GeneratedRegex("""^interface \[GrainInterfaceType\("(?<type>(?:[^"\\]|\\.)*)"\)\] (?<name>.+) \[Version\((?<version>\d+)\)\]$""", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex InterfaceLine { get; }

    [GeneratedRegex("""^class \[GrainType\("(?<type>(?:[^"\\]|\\.)*)"\)\] (?<name>.+)$""", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex ClassLine { get; }

    [GeneratedRegex(@"\\(?<character>.)", RegexOptions.None, MatchTimeoutMilliseconds)]
    private static partial Regex EscapedCharacter { get; }

    /// <summary>
    /// Reads a manifest from its text.
    /// </summary>
    /// <param name="text">The content of an OrleansContracts.txt.</param>
    /// <returns>The <see cref="ContractManifest"/>.</returns>
    /// <exception cref="InvalidContractManifest">Thrown when a line cannot be read.</exception>
    public static ContractManifest Parse(string text)
    {
        var interfaces = new Dictionary<string, GrainInterfaceContract>(StringComparer.Ordinal);
        var classes = new Dictionary<string, GrainClassContract>(StringComparer.Ordinal);
        string? currentInterface = null;
        Dictionary<string, GrainMethodContract>? currentMethods = null;

        var lines = text.ReplaceLineEndings("\n").Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index].TrimEnd();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith(MethodIndent, StringComparison.Ordinal))
            {
                if (currentMethods is null)
                {
                    throw new InvalidContractManifest(lineNumber, "A method appears before any interface.");
                }

                var method = ParseMethod(lineNumber, line[MethodIndent.Length..]);
                if (!currentMethods.TryAdd(method.Identity, method))
                {
                    throw new InvalidContractManifest(lineNumber, $"Method identity '{method.Identity}' appears twice on {currentInterface}.");
                }

                continue;
            }

            var retired = line.StartsWith(RetiredPrefix, StringComparison.Ordinal);
            var declaration = retired ? line[RetiredPrefix.Length..] : line;

            if (InterfaceLine.Match(declaration) is { Success: true } interfaceMatch)
            {
                var interfaceType = Unescape(interfaceMatch.Groups["type"].Value);
                currentMethods = new Dictionary<string, GrainMethodContract>(StringComparer.Ordinal);
                var contract = new GrainInterfaceContract(
                    interfaceType,
                    interfaceMatch.Groups["name"].Value,
                    int.Parse(interfaceMatch.Groups["version"].Value, CultureInfo.InvariantCulture),
                    retired,
                    currentMethods);

                if (!interfaces.TryAdd(interfaceType, contract))
                {
                    throw new InvalidContractManifest(lineNumber, $"Interface '{interfaceType}' is declared twice.");
                }

                currentInterface = interfaceType;
                continue;
            }

            if (ClassLine.Match(declaration) is { Success: true } classMatch)
            {
                var grainType = Unescape(classMatch.Groups["type"].Value);
                if (!classes.TryAdd(grainType, new GrainClassContract(grainType, classMatch.Groups["name"].Value, retired)))
                {
                    throw new InvalidContractManifest(lineNumber, $"Grain class '{grainType}' is declared twice.");
                }

                currentInterface = null;
                currentMethods = null;
                continue;
            }

            throw new InvalidContractManifest(lineNumber, $"Not an interface, class or method declaration: '{line}'.");
        }

        return new ContractManifest(interfaces, classes);
    }

    static GrainMethodContract ParseMethod(int lineNumber, string line)
    {
        var separator = IndexOfUnescapedSeparator(line);
        if (separator <= 0)
        {
            throw new InvalidContractManifest(lineNumber, $"A method line has no wire identity: '{line}'.");
        }

        var identity = Unescape(line[..separator]);
        var rest = line[(separator + 2)..];
        var parenthesis = rest.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis <= 0)
        {
            throw new InvalidContractManifest(lineNumber, $"A method line has no parameter list: '{line}'.");
        }

        return new GrainMethodContract(identity, rest[..parenthesis], rest[parenthesis..]);
    }

    static int IndexOfUnescapedSeparator(string line)
    {
        for (var index = 0; index < line.Length - 1; index++)
        {
            if (line[index] == '\\')
            {
                index++;
                continue;
            }

            if (line[index] == ':' && line[index + 1] == ' ')
            {
                return index;
            }
        }

        return -1;
    }

    static string Unescape(string value) => EscapedCharacter.Replace(value, "${character}");
}
