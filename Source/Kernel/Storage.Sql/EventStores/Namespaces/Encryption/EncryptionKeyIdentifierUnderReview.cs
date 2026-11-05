// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Exception that gets thrown when an encryption key identifier differs only in letter case, width, accents or trailing
/// spaces from an identifier that was stored before identifiers were compared exactly.
/// </summary>
/// <remarks>
/// The identifier is deliberately not part of the message.
/// </remarks>
public class EncryptionKeyIdentifierUnderReview()
    : Exception("The encryption key identifier differs only in letter case, width or trailing spaces from an identifier stored before identifiers were compared exactly. It is refused until the matching rows in EncryptionKeyIdentifierReviews have been reviewed; see the compliance storage documentation.");
