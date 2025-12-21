// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Security.Cryptography;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage.Security;
using Cratis.Reactive;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IClientCredentials"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientCredentials"/> class.
/// </remarks>
/// <param name="clientCredentialsStorage">The <see cref="IClientCredentialsStorage"/> for working with client credentials.</param>
internal sealed class ClientCredentials(IClientCredentialsStorage clientCredentialsStorage) : IClientCredentials
{
    /// <inheritdoc/>
    public async Task Add(AddClientCredentials command)
    {
        var clientSecret = HashSecret(command.ClientSecret);
        
        var client = new ChronicleClient(
            command.Id,
            command.ClientId,
            clientSecret,
            true,
            DateTimeOffset.UtcNow,
            null);

        await clientCredentialsStorage.Create(client);
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveClientCredentials command)
    {
        await clientCredentialsStorage.Delete(command.Id);
    }

    /// <inheritdoc/>
    public async Task ChangeSecret(ChangeClientCredentialsSecret command)
    {
        var client = await clientCredentialsStorage.GetById(command.Id);
        if (client is not null)
        {
            var clientSecret = HashSecret(command.ClientSecret);
            var updatedClient = client with
            {
                ClientSecret = clientSecret,
                LastModifiedAt = DateTimeOffset.UtcNow
            };
            await clientCredentialsStorage.Update(updatedClient);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Security.ClientCredentials>> GetAll()
    {
        var clients = await clientCredentialsStorage.GetAll();
        return clients.Select(ToContract);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Contracts.Security.ClientCredentials>> ObserveAll(CallContext context = default) =>
        clientCredentialsStorage
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(clients => clients.Select(ToContract).ToArray());

    static string HashSecret(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hashed = KeyDerivation.Pbkdf2(
            password: secret,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hashed);
    }

    static Contracts.Security.ClientCredentials ToContract(ChronicleClient client) => new()
    {
        Id = client.Id,
        ClientId = client.ClientId,
        IsActive = client.IsActive,
        CreatedAt = client.CreatedAt,
        LastModifiedAt = client.LastModifiedAt
    };
}
