// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Conventions;

/// <summary>
/// Exception that gets thrown when a signature is not a delegate type.
/// </summary>
public class SignatureNotDelegate : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureNotDelegate"/> class.
    /// </summary>
    /// <param name="signature">Type that is invalid.</param>
    public SignatureNotDelegate(Type signature) : base($"Signature '{signature.FullName}' is not a delegate type")
    {
    }

    /// <summary>
    /// Throw if the signature is invalid.
    /// </summary>
    /// <param name="signature">Type to check.</param>
    /// <exception cref="SignatureNotDelegate"><see cref="SignatureNotDelegate"/> is thrown if the type is not a delegate.</exception>
    public static void ThrowIfInvalid(Type signature)
    {
        if (signature.BaseType != typeof(MulticastDelegate))
        {
            throw new SignatureNotDelegate(signature);
        }
    }
}
