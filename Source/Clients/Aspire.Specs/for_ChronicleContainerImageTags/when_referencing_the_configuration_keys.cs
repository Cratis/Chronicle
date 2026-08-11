// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aspire.for_ChronicleContainerImageTags;

/// <summary>
/// Pins the container environment variable keys as literals. The Chronicle server binds them through the
/// <c>Cratis__Chronicle__</c> prefix onto <c>ChronicleOptions</c>, so a renamed constant would leave every
/// other spec green while the server silently ignored the configuration.
/// </summary>
public class when_referencing_the_configuration_keys : Specification
{
    [Fact] void should_configure_the_storage_type() => ChronicleContainerImageTags.StorageTypeEnvironmentVariable.ShouldEqual("Cratis__Chronicle__Storage__Type");
    [Fact] void should_configure_the_storage_connection_details() => ChronicleContainerImageTags.StorageConnectionDetailsEnvironmentVariable.ShouldEqual("Cratis__Chronicle__Storage__ConnectionDetails");
    [Fact] void should_configure_the_compliance_encryption_storage_type() => ChronicleContainerImageTags.ComplianceEncryptionStorageTypeEnvironmentVariable.ShouldEqual("Cratis__Chronicle__Compliance__Encryption__Storage__Type");
    [Fact] void should_configure_the_compliance_encryption_storage_connection_details() => ChronicleContainerImageTags.ComplianceEncryptionStorageConnectionDetailsEnvironmentVariable.ShouldEqual("Cratis__Chronicle__Compliance__Encryption__Storage__ConnectionDetails");
    [Fact] void should_configure_the_vault_token() => ChronicleContainerImageTags.VaultTokenEnvironmentVariable.ShouldEqual("VAULT_TOKEN");
    [Fact] void should_configure_the_tls_certificate_path() => ChronicleContainerImageTags.TlsCertificatePathEnvironmentVariable.ShouldEqual("Cratis__Chronicle__Tls__CertificatePath");
    [Fact] void should_configure_the_tls_certificate_password() => ChronicleContainerImageTags.TlsCertificatePasswordEnvironmentVariable.ShouldEqual("Cratis__Chronicle__Tls__CertificatePassword");
    [Fact] void should_configure_the_encryption_certificate_path() => ChronicleContainerImageTags.EncryptionCertificatePathEnvironmentVariable.ShouldEqual("Cratis__Chronicle__EncryptionCertificate__CertificatePath");
    [Fact] void should_configure_the_encryption_certificate_password() => ChronicleContainerImageTags.EncryptionCertificatePasswordEnvironmentVariable.ShouldEqual("Cratis__Chronicle__EncryptionCertificate__CertificatePassword");
    [Fact] void should_mount_the_tls_certificate_below_certs() => ChronicleContainerImageTags.TlsCertificateContainerPath.ShouldEqual("/certs/tls.pfx");
    [Fact] void should_mount_the_encryption_certificate_below_certs() => ChronicleContainerImageTags.EncryptionCertificateContainerPath.ShouldEqual("/certs/encryption.pfx");
}
