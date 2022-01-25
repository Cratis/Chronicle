// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution
{
    public class when_setting_current_statically : Specification
    {
        ExecutionContext new_context = new(Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid());

        public when_setting_current_statically()
        {
            // Since the specification runner is using IAsyncLifetime - it will be in a different async context.
            // Use default behavior, since we need to have control over the async context.
            ExecutionContextManager.SetCurrent(new_context);
        }

        [Fact] void should_hold_the_new_context() => ExecutionContextManager.GetCurrent().ShouldEqual(new_context);
    }
}
