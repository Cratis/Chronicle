// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_IPIIManager.given;

public class two_implementations : Specification
{
    protected static readonly EncryptionKeyIdentifier Identifier = "person-42";

    /// <summary>
    /// Stands in for an implementation written before <see cref="IPIIManager.AllowNewEncryptionKeyFor"/> existed.
    /// It compiles without overriding the member, which is the whole point of the default implementation.
    /// </summary>
    protected sealed class predates_the_member : IPIIManager
    {
        public Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier) => Task.CompletedTask;
    }

    protected sealed class supports_the_member : IPIIManager
    {
        public EncryptionKeyIdentifier? AuthorizedFor { get; private set; }

        public Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier) => Task.CompletedTask;

        public Task AllowNewEncryptionKeyFor(EncryptionKeyIdentifier identifier)
        {
            AuthorizedFor = identifier;
            return Task.CompletedTask;
        }
    }
}
