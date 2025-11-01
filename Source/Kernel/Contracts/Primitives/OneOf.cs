// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Primitives;

#pragma warning disable SA1402 // File may contain multiple types
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

/// <summary>
/// Represents a construct for one of three types.
/// </summary>
/// <typeparam name="T0">Type of first type.</typeparam>
/// <typeparam name="T1">Type of second type.</typeparam>
/// <typeparam name="T2">Type of third type.</typeparam>
[ProtoContract]
public class OneOf<T0, T1, T2>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2}"/> class.
    /// </summary>
    public OneOf()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2}"/> class.
    /// </summary>
    /// <param name="value">First type value.</param>
    public OneOf(T0 value)
    {
        Value0 = value;
        Value1 = default;
        Value2 = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2}"/> class.
    /// </summary>
    /// <param name="value">Second type value.</param>
    public OneOf(T1 value)
    {
        Value0 = default;
        Value1 = value;
        Value2 = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2}"/> class.
    /// </summary>
    /// <param name="value">Third type value.</param>
    public OneOf(T2 value)
    {
        Value0 = default;
        Value1 = default;
        Value2 = value;
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
    /// Second value as second type.
    /// </summary>
    [ProtoMember(3, IsRequired = false)]
    public T2? Value2 { get; set; }

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
            if (Value2 is not null)
            {
                return Value2;
            }

            throw new NoValueSetForOneOf(typeof(T0), typeof(T1), typeof(T2));
        }
    }
}

/// <summary>
/// Represents a construct for one of four types.
/// </summary>
/// <typeparam name="T0">Type of first type.</typeparam>
/// <typeparam name="T1">Type of second type.</typeparam>
/// <typeparam name="T2">Type of third type.</typeparam>
/// <typeparam name="T3">Type of fourth type.</typeparam>
[ProtoContract]
public class OneOf<T0, T1, T2, T3>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2, T3}"/> class.
    /// </summary>
    public OneOf()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2, T3}"/> class.
    /// </summary>
    /// <param name="value">First type value.</param>
    public OneOf(T0 value)
    {
        Value0 = value;
        Value1 = default;
        Value2 = default;
        Value3 = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2, T3 }"/> class.
    /// </summary>
    /// <param name="value">Second type value.</param>
    public OneOf(T1 value)
    {
        Value0 = default;
        Value1 = value;
        Value2 = default;
        Value3 = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2, T3}"/> class.
    /// </summary>
    /// <param name="value">Third type value.</param>
    public OneOf(T2 value)
    {
        Value0 = default;
        Value1 = default;
        Value2 = value;
        Value3 = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOf{T0, T1, T2, T3}"/> class.
    /// </summary>
    /// <param name="value">Third type value.</param>
    public OneOf(T3 value)
    {
        Value0 = default;
        Value1 = default;
        Value2 = default;
        Value3 = value;
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
    /// Second value as second type.
    /// </summary>
    [ProtoMember(3, IsRequired = false)]
    public T2? Value2 { get; set; }

    /// <summary>
    /// Second value as second type.
    /// </summary>
    [ProtoMember(4, IsRequired = false)]
    public T3? Value3 { get; set; }

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
            if (Value2 is not null)
            {
                return Value2;
            }
            if (Value3 is not null)
            {
                return Value3;
            }

            throw new NoValueSetForOneOf(typeof(T0), typeof(T1), typeof(T2), typeof(T3));
        }
    }
}
#pragma warning restore SA1402 // File may contain multiple types
