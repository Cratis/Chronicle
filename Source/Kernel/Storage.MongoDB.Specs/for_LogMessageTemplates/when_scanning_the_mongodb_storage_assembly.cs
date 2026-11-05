// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.for_LogMessageTemplates;

public class when_scanning_the_mongodb_storage_assembly : Specification
{
    static readonly string[] _denied = ["{EventSourceId}", "{Identifier}", "{Subject}", "{UserName}", "{Partition}", "{Key}", "{Id}"];

    string[] _offenders;
    int _scanned;

    void Because()
    {
        var messages = typeof(Sink).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(method => (Name: $"{method.DeclaringType!.FullName}.{method.Name}", Attribute: method.GetCustomAttribute<LoggerMessageAttribute>()))
            .Where(_ => _.Attribute is not null)
            .ToArray();
        _scanned = messages.Length;
        _offenders = [.. messages
            .Where(_ => _denied.Any(placeholder => (_.Attribute!.Message ?? string.Empty).Contains(placeholder, StringComparison.Ordinal)))
            .Select(_ => _.Name)];
    }

    [Fact] void should_find_log_messages_to_scan() => _scanned.ShouldBeGreaterThan(0);
    [Fact] void should_not_have_identifier_values_in_any_log_message() => _offenders.ShouldBeEmpty();
}
