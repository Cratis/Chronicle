// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;

namespace Aksio.Cratis.Reflection
{
    /// <summary>
    /// Extension methods for calling methods on objects using reflection.
    /// </summary>
    public static class MethodCalls
    {
        /// <summary>
        /// Call generic method.
        /// </summary>
        /// <param name="target">Target to call method on.</param>
        /// <param name="method">Signature of the method.</param>
        /// <param name="param1">First parameter.</param>
        /// <param name="param2">Second parameter.</param>
        /// <param name="param3">Third parameter.</param>
        /// <param name="genericArguments">Generic arguments.</param>
        /// <typeparam name="TOut">Output type.</typeparam>
        /// <typeparam name="T">Type of target.</typeparam>
        /// <typeparam name="T1">Type of first parameter.</typeparam>
        /// <typeparam name="T2">Type of second parameter.</typeparam>
        /// <typeparam name="T3">Type of third parameter.</typeparam>
        /// <returns>The result from the method call.</returns>
        public static TOut CallGenericMethod<TOut, T, T1, T2, T3>(this T target, Expression<Func<T, Func<T1, T2, T3, TOut>>> method, T1 param1, T2 param2, T3 param3, params Type[] genericArguments)
        {
            return CallGenericMethod<T, TOut>(target, method, new object[] { param1!, param2!, param3! }, genericArguments);
        }

        /// <summary>
        /// Call generic method.
        /// </summary>
        /// <param name="target">Target to call method on.</param>
        /// <param name="method">Signature of the method.</param>
        /// <param name="param1">First parameter.</param>
        /// <param name="param2">Second parameter.</param>
        /// <param name="genericArguments">Generic arguments.</param>
        /// <typeparam name="TOut">Output type.</typeparam>
        /// <typeparam name="T">Type of target.</typeparam>
        /// <typeparam name="T1">Type of first parameter.</typeparam>
        /// <typeparam name="T2">Type of second parameter.</typeparam>
        /// <returns>The result from the method call.</returns>
        public static TOut CallGenericMethod<TOut, T, T1, T2>(this T target, Expression<Func<T, Func<T1, T2, TOut>>> method, T1 param1, T2 param2, params Type[] genericArguments)
        {
            return CallGenericMethod<T, TOut>(target, method, new object[] { param1!, param2! }, genericArguments);
        }

        /// <summary>
        /// Call generic method.
        /// </summary>
        /// <param name="target">Target to call method on.</param>
        /// <param name="method">Signature of the method.</param>
        /// <param name="param">Method parameter.</param>
        /// <param name="genericArguments">Generic arguments.</param>
        /// <typeparam name="TOut">Output type.</typeparam>
        /// <typeparam name="T">Type of target.</typeparam>
        /// <typeparam name="T1">Type of parameter.</typeparam>
        /// <returns>The result from the method call.</returns>
        public static TOut CallGenericMethod<TOut, T, T1>(this T target, Expression<Func<T, Func<T1, TOut>>> method, T1 param, params Type[] genericArguments)
        {
            return CallGenericMethod<T, TOut>(target, method, new object[] { param! }, genericArguments);
        }

        /// <summary>
        /// Call generic method.
        /// </summary>
        /// <param name="target">Target to call method on.</param>
        /// <param name="method">Signature of the method.</param>
        /// <param name="genericArguments">Generic arguments.</param>
        /// <typeparam name="TOut">Output type.</typeparam>
        /// <typeparam name="T">Type of target.</typeparam>
        /// <returns>The result from the method call.</returns>
        public static TOut CallGenericMethod<TOut, T>(this T target, Expression<Func<T, Func<TOut>>> method, params Type[] genericArguments)
        {
            return CallGenericMethod<T, TOut>(target, method, Array.Empty<object>(), genericArguments);
        }

        static TOut CallGenericMethod<T, TOut>(this T target, Expression method, object[] parameters, Type[] genericArguments)
        {
            var lambda = method as LambdaExpression;
            var unary = lambda!.Body as UnaryExpression;
            var methodCall = unary!.Operand as MethodCallExpression;
            var constant = methodCall!.Object as ConstantExpression;

            var methodInfo = constant!.Value as MethodInfo;
            var genericMethodDefinition = methodInfo!.GetGenericMethodDefinition();

            var genericMethod = genericMethodDefinition.MakeGenericMethod(genericArguments);

            var result = genericMethod.Invoke(target, parameters);
            return (TOut)result!;
        }
    }
}
