// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Execution
{
    /// <summary>
    /// Exception that gets thrown when the <see cred="ExecutionContext"/> is not set.
    /// </summary>
    public class ExecutionContextNotSet : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ExecutionContextNotSet"/>.
        /// </summary>
        public ExecutionContextNotSet() : base("Execution context is not set")
        {
        }

        /// <summary>
        /// Throw <see cref="ExecutionContextNotSet"/> is not set.
        /// </summary>
        /// <param name="context"><see cref="ExecutionContext"/> to check.</param>
        /// <exception cref="ExecutionContextNotSet">Thrown if <see cref="ExecutionContext"/> is not set.</exception>
        public static void ThrowIfNotSet(ExecutionContext context)
        {
            if (context == null) throw new ExecutionContextNotSet();
        }
    }
}
