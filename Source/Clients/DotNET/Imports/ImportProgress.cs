// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents how far an import has come.
/// </summary>
/// <param name="ImportId">The <see cref="Imports.ImportId"/>.</param>
/// <param name="State">The <see cref="ImportState"/>.</param>
/// <param name="EventsReceived">How many events the kernel has received.</param>
/// <param name="EventsStaged">How many events are staged.</param>
/// <param name="EventsCommitted">How many events were committed.</param>
/// <param name="AlreadyAppended">How many events were already appended under their idempotency key.</param>
/// <param name="EventsRejected">How many events were rejected.</param>
public record ImportProgress(
    ImportId ImportId,
    ImportState State,
    long EventsReceived,
    long EventsStaged,
    long EventsCommitted,
    long AlreadyAppended,
    long EventsRejected);
