// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>Validates explicitly addressed principal, ordinary store and access flags.</summary>
internal class GrantEventStoreAccessValidator : CommandValidator<GrantEventStoreAccess>
{
    /// <summary>Initializes a new instance of the <see cref="GrantEventStoreAccessValidator"/> class.</summary>
    public GrantEventStoreAccessValidator()
    {
        RuleFor(command => command.Holder).NotNull();
        When(command => command.Holder is not null, () =>
        {
            RuleFor(command => command.Holder.Issuer).NotEmpty();
            RuleFor(command => command.Holder.Subject).NotEmpty();
        });
        RuleFor(command => command).Must(command => command.AllEventStores == (command.EventStore is null));
        RuleFor(command => command).Must(command => command.EventStore is null || (!string.IsNullOrWhiteSpace(command.EventStore.Value) && !SystemStoreAccess.IsSystem(command.EventStore)));
        RuleFor(command => command).Must(command => command.Namespace is null || !string.IsNullOrWhiteSpace(command.Namespace.Value));
        RuleFor(command => command.Access).Must(access => access != StoreAccess.None && (access & ~(StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage)) == StoreAccess.None);
    }
}
