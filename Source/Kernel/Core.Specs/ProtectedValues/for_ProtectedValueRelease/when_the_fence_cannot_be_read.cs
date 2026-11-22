// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_the_fence_cannot_be_read : Specification
{
    static readonly EncryptionKeyIdentifier _subject = "person-6035@example.invalid";
    IEncryptionKeyStorage _storage;
    IEncryption _encryption;
    JsonNode _released;
    string _warning;

    void Establish()
    {
        _storage = Substitute.For<IEncryptionKeyStorage>();
        _encryption = Substitute.For<IEncryption>();
        _storage.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _subject)
            .ThrowsAsync(new EncryptionKeyFenceUnavailable(_subject));
    }

    async Task Because()
    {
        await using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var listener = new TextWriterTraceListener(TextWriter.Synchronized(writer));
        Trace.Listeners.Add(listener);
        try
        {
            using var scope = ReleaseKeys.BeginScope();
            _released = await new ProtectedValueRelease(_storage, _encryption, (EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _subject), []).ReleaseOrEmpty();
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        _warning = writer.ToString();
    }

    [Fact] void should_release_empty() => _released.ShouldBeNull();
    [Fact] void should_warn_that_the_fence_could_not_be_validated() => _warning.ShouldContain("A protected value was released empty because its erasure fence could not be validated");
    [Fact] void should_record_only_the_exception_type() => _warning.ShouldContain(nameof(EncryptionKeyFenceUnavailable));
    [Fact] void should_not_name_the_subject() => _warning.ShouldNotContain(_subject.Value);
    [Fact] void should_not_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
