// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.BDD
{
    /// <summary>
    /// Represents a wrapper for working with exceptions.
    /// </summary>
    public static class Catch
    {
        /// <summary>
        /// Catch any exception that occurs from the wrapped callback.
        /// </summary>
        /// <param name="callback">Callback to wrap.</param>
        /// <returns>Exception that happened - if any. Null if not.</returns>
        public static Exception? Exception(Action callback)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        /// <summary>
        /// Catch a specific exception that occurs from the wrapped callback.
        /// </summary>
        /// <typeparam name="T">Type of exception to catch.</typeparam>
        /// <param name="callback">Callback to wrap.</param>
        /// <returns>Exception that happened - if any. Null if not.</returns>
        public static T? Exception<T>(Action callback)
            where T:Exception
        {
            try
            {
                callback();
            }
            catch (T ex)
            {
                return ex;
            }

            return null;
        }
    }
}
