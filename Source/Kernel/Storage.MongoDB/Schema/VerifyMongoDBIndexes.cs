// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Warns about missing MongoDB indexes in the fixed startup scopes without DDL or failing startup.
/// </summary>
public class VerifyMongoDBIndexes : IStartupTask
{
    readonly IOptions<ChronicleOptions> _options;
    readonly Func<CancellationToken, Task<MongoDBIndexReport>> _verify;
    readonly ILogger<VerifyMongoDBIndexes> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyMongoDBIndexes"/> class.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="provisioner">The index provisioner.</param>
    /// <param name="logger">The logger.</param>
    public VerifyMongoDBIndexes(IOptions<ChronicleOptions> options, MongoDBIndexProvisioner provisioner, ILogger<VerifyMongoDBIndexes> logger)
        : this(options, provisioner.VerifyStartup, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance with the restricted startup inspection callback.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="verify">The restricted startup inspection.</param>
    /// <param name="logger">The logger.</param>
    internal VerifyMongoDBIndexes(IOptions<ChronicleOptions> options, Func<CancellationToken, Task<MongoDBIndexReport>> verify, ILogger<VerifyMongoDBIndexes> logger)
    {
        _options = options;
        _verify = verify;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        if (_options.Value.Storage.SchemaMode != SchemaMode.Verify) return;
        try
        {
            var report = await _verify(cancellationToken);
            foreach (var scope in report.Scopes.Where(scope => scope.Missing.Count > 0 || scope.Obsolete.Count > 0))
            {
                _logger.IndexesMissing(scope.Scope, scope.Database, string.Join(", ", scope.Missing), string.Join(", ", scope.Obsolete));
            }
        }
        catch (Exception exception)
        {
            // Index verification is advisory for MongoDB, including connection and authorization failures.
            _logger.VerificationFailed(exception);
        }
    }
}
