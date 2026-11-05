// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_LogMessageTemplates;

public class when_scanning_storage_assemblies : Specification
{
    static readonly string[] _denied = ["{EventSourceId}", "{Identifier}", "{Subject}", "{UserName}", "{Partition}", "{Key}", "{Id}"];

    static readonly string[] _allowed =
    [

        // The identifier is the name of a column, not a value.
        "Cratis.Chronicle.Storage.Sql.Sinks.SinkLogging.LegacyKeyKept",

        // Temporary: the encryption key identifiers are kept out of the log in a later step, which removes this entry.
        "Cratis.Chronicle.Storage.Compliance.CompositeEncryptionKeyStorageLogMessages.ReadingFromInnerStoreFailed",
        "Cratis.Chronicle.Storage.Compliance.CompositeEncryptionKeyStorageLogMessages.HealingInnerStoreFailed",
        "Cratis.Chronicle.Storage.Compliance.CompositeEncryptionKeyStorageLogMessages.MirroringToInnerStoreFailed",
        "Cratis.Chronicle.Storage.Compliance.CompositeEncryptionKeyStorageLogMessages.InnerStoreHoldsDivergentKey",
        "Cratis.Chronicle.Storage.Compliance.CompositeEncryptionKeyStorageLogMessages.ErasedKeySurvivedInAnotherStore"
    ];

    string[] _offenders;
    int _scanned;

    void Because()
    {
        var messages = new[] { typeof(IStorage).Assembly, typeof(Database).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(method => (Name: $"{method.DeclaringType!.FullName}.{method.Name}", Attribute: method.GetCustomAttribute<LoggerMessageAttribute>()))
            .Where(_ => _.Attribute is not null)
            .ToArray();
        _scanned = messages.Length;
        _offenders = [.. messages
            .Where(_ => !_allowed.Contains(_.Name) && _denied.Any(placeholder => (_.Attribute!.Message ?? string.Empty).Contains(placeholder, StringComparison.Ordinal)))
            .Select(_ => _.Name)];
    }

    [Fact] void should_find_log_messages_to_scan() => _scanned.ShouldBeGreaterThan(0);
    [Fact] void should_not_have_identifier_values_in_any_log_message() => _offenders.ShouldBeEmpty();
}
