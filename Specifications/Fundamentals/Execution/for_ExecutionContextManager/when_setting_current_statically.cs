// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Execution
{
    public class when_setting_current_statically : Specification
    {
        ExecutionContext new_context = new(Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        void Because() => ExecutionContextManager.SetCurrent(new_context);

        [Fact] void should_hold_the_new_context() => ExecutionContextManager.GetCurrent().ShouldEqual(new_context);
    }
}
