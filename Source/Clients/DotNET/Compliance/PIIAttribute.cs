// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Compliance
{
    /// <summary>
    /// Represents an attribute that can be used to mark classes or properties to indicate the information kept is PII according to the definition of GDPR.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class PIIAttribute : Attribute
    {
    }
}
