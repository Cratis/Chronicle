// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Exception that gets thrown when an <see cref="EncryptionKey"/> is missing.
    /// </summary>
    public class MissingEncryptionKey : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingEncryptionKey"/> class.
        /// </summary>
        /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> that is missing.</param>
        public MissingEncryptionKey(EncryptionKeyIdentifier identifier) : base($"Missing encryption key for identifier '{identifier}'")
        {
        }
    }
}
