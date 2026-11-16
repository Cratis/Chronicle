// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class an_unrecoverable_pii_value : a_type_with_one_property
{
    protected const string Subject = "subject";
    protected JsonSchemaMetadataManager _manager;
    protected ILogger<JsonSchemaMetadataManager> _logger;
    protected EncryptionKeyUnwrapFailed _unwrap;
    protected int _count;

    void Establish()
    {
        var storage = Substitute.For<IEncryptionKeyStorage>();
        var encryption = Substitute.For<IEncryption>();
        var latest = new EncryptionKey([1], [2]);
        var retained = new EncryptionKey([3], [4]);
        _unwrap = new(new CryptographicException("sensitive cryptographic details"));
        storage.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.Default, Subject).Returns(new EncryptionKeyAtRevision(latest, 2));
        storage.TryGetAtRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.Default, Subject, EncryptionKeyRevision.Initial).Returns(new EncryptionKeyAtRevision(retained, 1));
        encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        encryption.Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>()).Returns(_ => throw _unwrap);
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(storage, encryption), storage, encryption);
        _schema.Properties[PropertyName].ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata(handler.Type, string.Empty) }
        };
        _input[PropertyName] = Convert.ToBase64String(new byte[256]);
        _logger = Substitute.For<ILogger<JsonSchemaMetadataManager>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var reporter = new UnrecoverableProtectedValueReporter(_logger, TimeProvider.System, () => _count++);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), _logger, reporter);
    }
}
