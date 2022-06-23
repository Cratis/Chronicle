// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.AccountHolders;

namespace Integration.AccountHolders.for_AccountHolderDetailsAdapter;

public class when_importing_for_the_second_time_without_changes : given.object_ready_for_import
{
    void Establish()
    {
        context.EventLog.Append(social_security_number, new AccountHolderRegistered(first_name, last_name, birth_date, new(address, city, postal_code, country)));
        context.EventLog.Append(social_security_number, new AccountHolderAddressChanged(address, city, postal_code, country));
    }

    Task Because() => context.Import(object_to_import);

    [Fact] void should_not_append_any_events() => context.ShouldNotAppendEventsDuringImport();
}
