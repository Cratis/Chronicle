// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Primitives;

/// <summary>
/// Represents a construct for one of two types.
/// </summary>
/// <typeparam name="T0">Type of first type.</typeparam>
/// <typeparam name="T1">Type of second type.</typeparam>
[ProtoContract]
public class OneOf<T0, T1>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1}"/> class.
    /// </summary>
    public OneOf()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1}"/> class.
    /// </summary>
    /// <param name="value">First type value.</param>
    public OneOf(T0 value)
    {
        Value0 = value;
        Value1 = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1}"/> class.
    /// </summary>
    /// <param name="value">Second type value.</param>
    public OneOf(T1 value)
    {
        Value0 = default;
        Value1 = value;
    }

    /// <summary>
    /// First value as first type.
    /// </summary>
    [ProtoMember(1, IsRequired = false)]
    public T0? Value0 { get; set; }

    /// <summary>
    /// Second value as second type.
    /// </summary>
    [ProtoMember(2, IsRequired = false)]
    public T1? Value1 { get; set; }

    /// <summary>
    /// Get the value that is set.
    /// </summary>
    /// <exception cref="NoValueSetForOneOf">Thrown if no value is set.</exception>
    [ProtoIgnore]
    public object Value
    {
        get
        {
            if (Value0 is not null)
            {
                return Value0;
            }
            if (Value1 is not null)
            {
                return Value1;
            }

            throw new NoValueSetForOneOf(typeof(T0), typeof(T1));
        }
    }
}
