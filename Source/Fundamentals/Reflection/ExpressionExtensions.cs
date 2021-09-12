// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;

namespace Cratis.Reflection
{
    /// <summary>
    /// Provides methods for working with expressions.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// A Func to extract a member expression from an Expression.
        /// </summary>
        public static Func<Expression, MemberExpression> Unwrap { get; } = toUnwrap =>
                                                                            {
                                                                                if (toUnwrap is UnaryExpression unwrap)
                                                                                    return (MemberExpression)unwrap.Operand!;

                                                                                return (MemberExpression)toUnwrap!;
                                                                            };

        /// <summary>
        /// Get <see cref="MethodInfo">MethodInfo</see> from an <see cref="Expression">expression</see> - if any.
        /// </summary>
        /// <param name="expression"><see cref="Expression">Expression</see> to get MethodInfo from.</param>
        /// <returns>The <see cref="MethodInfo">MethodInfo</see> found, null if did not find one.</returns>
        public static MethodInfo GetMethodInfo(this Expression expression)
        {
            if (expression is LambdaExpression lambda &&
                lambda.Body is MethodCallExpression)
            {
                var methodCall = lambda.Body as MethodCallExpression;
                return methodCall!.Method;
            }

            return null!;
        }

        /// <summary>
        /// Get all argument instances from a method expression.
        /// </summary>
        /// <param name="expression"><see cref="Expression"/> to get argument instances from.</param>
        /// <returns>Array of argument instances.</returns>
        public static object[] GetMethodArguments(this Expression expression)
        {
            if (expression is LambdaExpression lambda &&
                lambda.Body is MethodCallExpression)
            {
                var methodCall = lambda.Body as MethodCallExpression;
                var arguments = new List<object>();

                foreach (var argument in methodCall!.Arguments)
                {
                    var member = argument as MemberExpression;
                    var value = member!.GetInstance();
                    arguments.Add(value);
                }

                return arguments.ToArray();
            }

            return Array.Empty<object>();
        }

        /// <summary>
        /// Get <see cref="MemberExpression">MemberExpression</see> from an <see cref="Expression">expression</see> - if any.
        /// </summary>
        /// <param name="expression"><see cref="Expression">Expression</see> to get <see cref="MemberExpression">MemberExpression</see> from.</param>
        /// <returns><see cref="MemberExpression">MemberExpression</see> instance, null if there is none.</returns>
        public static MemberExpression GetMemberExpression(this Expression expression)
        {
            var lambda = expression as LambdaExpression;
            if (lambda?.Body is UnaryExpression)
            {
                var unaryExpression = lambda.Body as UnaryExpression;
                return (MemberExpression)unaryExpression!.Operand!;
            }

            return (MemberExpression)lambda?.Body!;
        }

        /// <summary>
        /// Get <see cref="FieldInfo">FieldInfo</see> from an <see cref="Expression">Expression</see> - if any.
        /// </summary>
        /// <param name="expression"><see cref="Expression">Expression</see> to get <see cref="FieldInfo">FieldInfo</see> from.</param>
        /// <returns><see cref="FieldInfo">FieldInfo</see> instance, null if there is none.</returns>
        public static FieldInfo GetFieldInfo(this Expression expression)
        {
            var memberExpression = GetMemberExpression(expression);
            return (FieldInfo)memberExpression.Member!;
        }

        /// <summary>
        /// Get <see cref="PropertyInfo">PropertyInfo</see> from an <see cref="Expression">Expression</see> - if any.
        /// </summary>
        /// <param name="expression"><see cref="Expression">Expression</see> to get <see cref="PropertyInfo">PropertyInfo</see> from.</param>
        /// <returns><see cref="PropertyInfo">PropertyInfo</see> instance, null if there is none.</returns>
        public static PropertyInfo GetPropertyInfo(this Expression expression)
        {
            var memberExpression = GetMemberExpression(expression);
            return (PropertyInfo)memberExpression.Member!;
        }

        /// <summary>
        /// Get an instance reference from an <see cref="Expression">Expression</see> - if any.
        /// </summary>
        /// <param name="expression"><see cref="Expression">Expression</see> to get an instance from.</param>
        /// <returns>The instance, null if there is none.</returns>
        public static object GetInstance(this Expression expression)
        {
            var memberExpression = GetMemberExpression(expression);
            return GetInstance(memberExpression);
        }

        /// <summary>
        /// Get an instance reference from an <see cref="Expression">Expression</see>, with a specific type - if any.
        /// </summary>
        /// <typeparam name="T">Type of the instance.</typeparam>
        /// <param name="expression"><see cref="Expression">Expression</see> to get an instance from.</param>
        /// <returns>The instance, null if there is none.</returns>
        public static T GetInstance<T>(this Expression expression)
        {
            return (T)GetInstance(expression);
        }

        static object GetInstance(this MemberExpression memberExpression)
        {
            if (!(memberExpression.Expression is ConstantExpression constantExpression))
            {
                var innerMember = memberExpression.Expression as MemberExpression;
                if (innerMember!.Member is FieldInfo info)
                    return info.GetValue(null)!;

                constantExpression = (ConstantExpression)innerMember!.Expression!;
                return GetValue(innerMember, constantExpression!)!;
            }

            return GetValue(memberExpression, constantExpression);
        }

        static object GetValue(MemberExpression memberExpression, ConstantExpression constantExpression)
        {
            if (memberExpression.Member is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(constantExpression.Value, null)!;

            if (memberExpression.Member is FieldInfo fieldInfo)
                return fieldInfo.GetValue(constantExpression.Value)!;

            return constantExpression.Value!;
        }
    }
}
