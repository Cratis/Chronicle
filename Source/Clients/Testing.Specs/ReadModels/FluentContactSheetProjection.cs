// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Standalone fluent projection for <see cref="FluentContactSheet"/>, exercising a
/// <c>.Children(...).IdentifiedBy(...).From&lt;E&gt;(b =&gt; b.UsingKey(...))</c> child collection — the exact
/// shape a consumer reaches for and the one that previously failed to build under
/// <see cref="ReadModelScenario{TReadModel}"/>.
/// </summary>
public class FluentContactSheetProjection : IProjectionFor<FluentContactSheet>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<FluentContactSheet> builder) => builder
        .From<SheetStarted>()
        .Children(_ => _.Contacts, contacts => contacts
            .IdentifiedBy(_ => _.ContactId)
            .From<ContactAssigned>(from => from.UsingKey(e => e.ContactId)));
}
