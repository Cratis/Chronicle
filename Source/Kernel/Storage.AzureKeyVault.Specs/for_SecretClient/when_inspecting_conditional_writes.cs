// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretClient;

public class when_inspecting_conditional_writes : Specification
{
    MethodInfo[] _writes;
    ParameterInfo[] _conditions;
    PropertyInfo[] _entityTags;
    PropertyInfo[] _secretConditions;

    void Because()
    {
        _writes = typeof(SecretClient).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(_ => _.Name.Equals(nameof(SecretClient.SetSecret), StringComparison.Ordinal) ||
                        _.Name.Equals(nameof(SecretClient.SetSecretAsync), StringComparison.Ordinal))
            .ToArray();
        _conditions = _writes.SelectMany(_ => _.GetParameters())
            .Where(_ => typeof(RequestConditions).IsAssignableFrom(_.ParameterType) || HasConditionName(_.Name!))
            .ToArray();
        _entityTags = typeof(SecretProperties).GetProperties()
            .Where(_ => _.PropertyType == typeof(ETag) || HasConditionName(_.Name))
            .ToArray();
        _secretConditions = typeof(KeyVaultSecret).GetProperties()
            .Where(_ => typeof(RequestConditions).IsAssignableFrom(_.PropertyType) || HasConditionName(_.Name))
            .ToArray();
    }

    [Fact] void should_inspect_every_sync_and_async_write_overload() => _writes.Length.ShouldEqual(4);
    [Fact] void should_offer_no_write_condition_parameter() => _conditions.ShouldBeEmpty();
    [Fact] void should_offer_no_entity_tag_or_condition_on_secret_properties() => _entityTags.ShouldBeEmpty();
    [Fact] void should_offer_no_condition_on_the_secret_being_written() => _secretConditions.ShouldBeEmpty();

    static bool HasConditionName(string name) =>
        name.Contains("condition", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("etag", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("ifmatch", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("ifnonematch", StringComparison.OrdinalIgnoreCase);
}
