// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

internal static class ExternalMongoDBResetSafety
{
    internal static void Verify(string prefix, IEnumerable<string> beforeStartup, IEnumerable<string> afterStartup)
    {
        var names = afterStartup.ToHashSet(StringComparer.Ordinal);
        var expectedDatabase = $"{prefix}chronicle+main";
        var unprefixedNames = names.Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(name => name[prefix.Length..]).ToHashSet(StringComparer.Ordinal);
        var unexpectedDatabases = names.Except(beforeStartup, StringComparer.Ordinal)
            .Where(name => !name.StartsWith(prefix, StringComparison.Ordinal)
                && (name == "chronicle+main" || name.Contains("+es", StringComparison.Ordinal) || unprefixedNames.Contains(name)))
            .ToArray();

        if (string.IsNullOrEmpty(prefix) || !names.Contains(expectedDatabase) || unexpectedDatabases.Length > 0)
        {
            throw new ExternalMongoDBKernelPrefixNotVerified(expectedDatabase, unexpectedDatabases);
        }
    }
}
