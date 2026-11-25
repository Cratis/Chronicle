// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.given;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_system_store_reindex_is_requested : an_operation_policy
{
    void Establish()
    {
        _enforce = false;
        _enforceDestructive = true;
        _operation = new ReindexConstraints(EventStoreName.System, "ns", "log", []);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_only_allow_an_administrator(bool administrator)
    {
        _authorization.IsAuthorized(_principal).Returns(administrator);
        await AuthorizeOperation();
        _context.HasSucceeded.ShouldEqual(administrator);
    }
}
