// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation.Webhooks;
using Microsoft.AspNetCore.DataProtection;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Implements webhook secret encryption using ASP.NET Core Data Protection API.
/// </summary>
/// <param name="dataProtectionProvider">The data protection provider.</param>
public class WebhookSecretEncryption(IDataProtectionProvider dataProtectionProvider) : IWebhookSecretEncryption
{
    const string Purpose = "Chronicle.Webhooks.Secrets";
    readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    /// <inheritdoc/>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        return _protector.Protect(plainText);
    }

    /// <inheritdoc/>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return string.Empty;
        }

        return _protector.Unprotect(encryptedText);
    }
}
