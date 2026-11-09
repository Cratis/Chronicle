// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

/// <summary>
/// Invokes the actual internal server seam without granting production friend access or copying gate logic.
/// </summary>
internal static class ServerCommand
{
    internal static Task<int?> Run(IEncryptionKeyStoreUpgradeCheck check, IEncryptionKeyStoreMigrator migrator, LegacyKeyHandling handling, params string[] args)
    {
        var assembly = Assembly.Load("Cratis.Chronicle.Server");
        var command = assembly.GetType("Cratis.Chronicle.Server.EncryptionKeyCommand", throwOnError: true)!;
        var runner = assembly.GetType("Cratis.Chronicle.Server.EncryptionKeyCommandRunner", throwOnError: true)!;
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        var parse = command.GetMethod("Parse", Flags, [typeof(string[])]) ?? throw new MissingMethodException("Encryption key command parse seam changed.");
        var run = runner.GetMethod("Run", Flags, [typeof(IEncryptionKeyStoreUpgradeCheck), typeof(ILogger), command, typeof(TextWriter), typeof(IEncryptionKeyStoreMigrator), typeof(LegacyKeyHandling)])
            ?? throw new MissingMethodException("Encryption key command runner seam changed.");
        if (parse.ReturnType != command || run.ReturnType != typeof(Task<int?>))
        {
            throw new InvalidOperationException("Encryption key command seam return types changed.");
        }

        var parsed = parse.Invoke(null, [args]);
        return (Task<int?>)run.Invoke(null, [check, NullLogger.Instance, parsed, TextWriter.Null, migrator, handling])!;
    }
}
