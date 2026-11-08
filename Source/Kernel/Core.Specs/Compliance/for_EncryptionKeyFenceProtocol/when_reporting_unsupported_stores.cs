// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR.for_PIIManager.given;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyFenceProtocol;

public class when_reporting_unsupported_stores : Specification
{
    RecordingLogger<EncryptionKeyFenceProtocol> _logger;
    EncryptionKeyFenceProtocol _publisher;

    void Establish()
    {
        _logger = new();
        var vault = Substitute.For<IEncryptionKeyStorage>();
        vault.Capabilities.Returns([new("HELD Vault", EncryptionKeyFenceGuarantees.None)]);
        var azure = Substitute.For<IEncryptionKeyStorage>();
        azure.Capabilities.Returns([new("Azure without durable authority", EncryptionKeyFenceGuarantees.None)]);
        var sql = Substitute.For<IEncryptionKeyStorage>();
        sql.FenceGuarantees.Returns(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);
        sql.Capabilities.Returns([new("SQL", EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave)]);
        var keys = new CompositeEncryptionKeyStorage(vault, azure, sql);
        _publisher = new(Substitute.For<IStorage>(), keys, new InMemoryEncryptionKeyStorage(), Substitute.For<ILocalSiloDetails>(), Options.Create(new ClusterOptions()), Options.Create(new ClusterMembershipOptions()), Options.Create(new ChronicleOptions()), _logger, TimeProvider.System);
    }

    void Because() => _publisher.ReportCapabilities();

    [Fact] void should_warn_about_each_unsupported_store() => _logger.Entries.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(2);
    [Fact] void should_name_the_held_vault_store() => _logger.Entries.Any(_ => _.Level == LogLevel.Warning && _.Message.Contains("HELD Vault", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_non_durable_azure_store() => _logger.Entries.Any(_ => _.Level == LogLevel.Warning && _.Message.Contains("Azure without durable authority", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_warn_for_the_supported_store() => _logger.Entries.Any(_ => _.Level == LogLevel.Warning && _.Message.Contains("SQL", StringComparison.Ordinal)).ShouldBeFalse();

    void Destroy() => _publisher.Dispose();
}
