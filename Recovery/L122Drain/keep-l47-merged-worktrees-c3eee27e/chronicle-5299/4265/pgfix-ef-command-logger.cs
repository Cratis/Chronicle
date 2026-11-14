using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Diagnostics.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class RelationalCommandDiagnosticsLogger : DiagnosticsLogger<Command>, IRelationalCommandDiagnosticsLogger, IDiagnosticsLogger<Command>, IDiagnosticsLogger
{
	private DateTimeOffset _suppressCommandCreateExpiration;

	private DateTimeOffset _suppressCommandExecuteExpiration;

	private DateTimeOffset _suppressDataReaderClosingExpiration;

	private DateTimeOffset _suppressDataReaderDisposingExpiration;

	private readonly TimeSpan _loggingCacheTime;

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public RelationalCommandDiagnosticsLogger(ILoggerFactory loggerFactory, ILoggingOptions loggingOptions, DiagnosticSource diagnosticSource, LoggingDefinitions loggingDefinitions, IDbContextLogger contextLogger, IDbContextOptions contextOptions, IInterceptors? interceptors = null)
		: base(loggerFactory, loggingOptions, diagnosticSource, loggingDefinitions, contextLogger, interceptors)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		CoreOptionsExtension val = (CoreOptionsExtension)(((object)contextOptions.FindExtension<CoreOptionsExtension>()) ?? ((object)new CoreOptionsExtension()));
		_loggingCacheTime = val.LoggingCacheTime;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbCommand> CommandCreating(IRelationalConnection connection, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandCreateExpiration = startTime + _loggingCacheTime;
		EventDefinition<string> val = RelationalResources.LogCommandCreating((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandCorrelatedEventData eventData = BroadcastCommandCreating(connection.DbConnection, context, commandMethod, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreating(eventData, default(InterceptionResult<DbCommand>));
			}
		}
		return default(InterceptionResult<DbCommand>);
	}

	private CommandCorrelatedEventData BroadcastCommandCreating(DbConnection connection, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandCorrelatedEventData commandCorrelatedEventData = new CommandCorrelatedEventData((EventDefinitionBase)(object)definition, CommandCreating, connection, context, executeMethod, commandId, connectionId, async, startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandCorrelatedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandCorrelatedEventData;
		static string CommandCreating(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string> obj = (EventDefinition<string>)(object)val;
			CommandCorrelatedEventData commandCorrelatedEventData2 = (CommandCorrelatedEventData)(object)payload;
			return obj.GenerateMessage(commandCorrelatedEventData2.ExecuteMethod.ToString());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandCreated(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandCreated((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandCreated(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreated(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandCreated(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCreated, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCreated(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandInitialized(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandInitialized((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandInitialized(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandInitialized(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandInitialized(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandInitialized, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandInitialized(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbDataReader> CommandReaderExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuting(command, eventData, default(InterceptionResult<DbDataReader>));
			}
		}
		return default(InterceptionResult<DbDataReader>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<object> CommandScalarExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuting(command, eventData, default(InterceptionResult<object>));
			}
		}
		return default(InterceptionResult<object>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<int> CommandNonQueryExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuting(command, eventData, default(InterceptionResult<int>));
			}
		}
		return default(InterceptionResult<int>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<DbDataReader>> CommandReaderExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutingAsync(command, eventData, default(InterceptionResult<DbDataReader>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<DbDataReader>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<object>> CommandScalarExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutingAsync(command, eventData, default(InterceptionResult<object>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<object>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<int>> CommandNonQueryExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutingAsync(command, eventData, default(InterceptionResult<int>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<int>>);
	}

	private CommandEventData BroadcastCommandExecuting(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEventData commandEventData = new CommandEventData((EventDefinitionBase)(object)definition, CommandExecuting, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEventData;
		static string CommandExecuting(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, CommandType, int, string, string> obj = (EventDefinition<string, CommandType, int, string, string>)(object)val;
			CommandEventData commandEventData2 = (CommandEventData)(object)payload;
			return obj.GenerateMessage(commandEventData2.Command.Parameters.FormatParameters(commandEventData2.LogParameterValues), commandEventData2.Command.CommandType, commandEventData2.Command.CommandTimeout, Environment.NewLine, commandEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbDataReader CommandReaderExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual object? CommandScalarExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual int CommandNonQueryExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<DbDataReader> CommandReaderExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<object?> CommandScalarExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<int> CommandNonQueryExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	private CommandExecutedEventData BroadcastCommandExecuted(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, object methodResult, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandExecutedEventData commandExecutedEventData = new CommandExecutedEventData((EventDefinitionBase)(object)definition, CommandExecuted, connection, command, logCommandText, context, executeMethod, commandId, connectionId, methodResult, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandExecutedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandExecutedEventData;
		static string CommandExecuted(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandExecutedEventData commandExecutedEventData2 = (CommandExecutedEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandExecutedEventData2.Duration.TotalMilliseconds), commandExecutedEventData2.Command.Parameters.FormatParameters(commandExecutedEventData2.LogParameterValues), commandExecutedEventData2.Command.CommandType, commandExecutedEventData2.Command.CommandTimeout, Environment.NewLine, commandExecutedEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual void CommandError(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandFailed(command, eventData);
		}
	}

	private void LogCommandError(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual Task CommandErrorAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandFailedAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private CommandErrorEventData BroadcastCommandError(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandErrorEventData commandErrorEventData = new CommandErrorEventData((EventDefinitionBase)(object)definition, CommandError, connection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandErrorEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandErrorEventData;
		static string CommandError(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandErrorEventData commandErrorEventData2 = (CommandErrorEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandErrorEventData2.Duration.TotalMilliseconds), commandErrorEventData2.Command.Parameters.FormatParameters(commandErrorEventData2.LogParameterValues), commandErrorEventData2.Command.CommandType, commandErrorEventData2.Command.CommandTimeout, Environment.NewLine, commandErrorEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <inheritdoc />
	public virtual void CommandCanceled(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandCanceled(command, eventData);
		}
	}

	/// <inheritdoc />
	public virtual Task CommandCanceledAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCanceledAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private void LogCommandCanceled(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	private CommandEndEventData BroadcastCommandCanceled(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCanceled, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCanceled(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandEndEventData2.Duration.TotalMilliseconds), commandEndEventData2.Command.Parameters.FormatParameters(commandEndEventData2.LogParameterValues), commandEndEventData2.Command.CommandType, commandEndEventData2.Command.CommandTimeout, Environment.NewLine, commandEndEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderClosing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: false, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosing(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult> DataReaderClosingAsync(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: true, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosingAsync(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(ValueTask<InterceptionResult>);
	}

	private static string CreateDataReaderClosingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string> obj = (EventDefinition<string, string>)(object)definition;
		DataReaderClosingEventData dataReaderClosingEventData = (DataReaderClosingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderClosingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderClosingEventData.Command.Connection?.DataSource ?? "<Unknown>");
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderDisposing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime, TimeSpan duration)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderDisposingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string, int> val = RelationalResources.LogDisposingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource, (int)duration.TotalMilliseconds, (Exception)null);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			DataReaderDisposingEventData dataReaderDisposingEventData = new DataReaderDisposingEventData((EventDefinitionBase)(object)val, CreateDataReaderDisposingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, recordsAffected, readCount, startTime, duration);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderDisposingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderDisposing(command, dataReaderDisposingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	private static string CreateDataReaderDisposingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string, int> obj = (EventDefinition<string, string, int>)(object)definition;
		DataReaderDisposingEventData dataReaderDisposingEventData = (DataReaderDisposingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderDisposingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderDisposingEventData.Command.Connection?.DataSource ?? "<Unknown>", (int)dataReaderDisposingEventData.Duration.TotalMilliseconds, (Exception)null);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandCreate(DateTimeOffset now)
	{
		return now > _suppressCommandCreateExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandExecute(DateTimeOffset now)
	{
		return now > _suppressCommandExecuteExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderClose(DateTimeOffset now)
	{
		return now > _suppressDataReaderClosingExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderDispose(DateTimeOffset now)
	{
		return now > _suppressDataReaderDisposingExpiration;
	}
}
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Diagnostics.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class RelationalCommandDiagnosticsLogger : DiagnosticsLogger<Command>, IRelationalCommandDiagnosticsLogger, IDiagnosticsLogger<Command>, IDiagnosticsLogger
{
	private DateTimeOffset _suppressCommandCreateExpiration;

	private DateTimeOffset _suppressCommandExecuteExpiration;

	private DateTimeOffset _suppressDataReaderClosingExpiration;

	private DateTimeOffset _suppressDataReaderDisposingExpiration;

	private readonly TimeSpan _loggingCacheTime;

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public RelationalCommandDiagnosticsLogger(ILoggerFactory loggerFactory, ILoggingOptions loggingOptions, DiagnosticSource diagnosticSource, LoggingDefinitions loggingDefinitions, IDbContextLogger contextLogger, IDbContextOptions contextOptions, IInterceptors? interceptors = null)
		: base(loggerFactory, loggingOptions, diagnosticSource, loggingDefinitions, contextLogger, interceptors)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		CoreOptionsExtension val = (CoreOptionsExtension)(((object)contextOptions.FindExtension<CoreOptionsExtension>()) ?? ((object)new CoreOptionsExtension()));
		_loggingCacheTime = val.LoggingCacheTime;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbCommand> CommandCreating(IRelationalConnection connection, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandCreateExpiration = startTime + _loggingCacheTime;
		EventDefinition<string> val = RelationalResources.LogCommandCreating((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandCorrelatedEventData eventData = BroadcastCommandCreating(connection.DbConnection, context, commandMethod, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreating(eventData, default(InterceptionResult<DbCommand>));
			}
		}
		return default(InterceptionResult<DbCommand>);
	}

	private CommandCorrelatedEventData BroadcastCommandCreating(DbConnection connection, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandCorrelatedEventData commandCorrelatedEventData = new CommandCorrelatedEventData((EventDefinitionBase)(object)definition, CommandCreating, connection, context, executeMethod, commandId, connectionId, async, startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandCorrelatedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandCorrelatedEventData;
		static string CommandCreating(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string> obj = (EventDefinition<string>)(object)val;
			CommandCorrelatedEventData commandCorrelatedEventData2 = (CommandCorrelatedEventData)(object)payload;
			return obj.GenerateMessage(commandCorrelatedEventData2.ExecuteMethod.ToString());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandCreated(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandCreated((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandCreated(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreated(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandCreated(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCreated, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCreated(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandInitialized(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandInitialized((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandInitialized(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandInitialized(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandInitialized(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandInitialized, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandInitialized(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbDataReader> CommandReaderExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuting(command, eventData, default(InterceptionResult<DbDataReader>));
			}
		}
		return default(InterceptionResult<DbDataReader>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<object> CommandScalarExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuting(command, eventData, default(InterceptionResult<object>));
			}
		}
		return default(InterceptionResult<object>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<int> CommandNonQueryExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuting(command, eventData, default(InterceptionResult<int>));
			}
		}
		return default(InterceptionResult<int>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<DbDataReader>> CommandReaderExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutingAsync(command, eventData, default(InterceptionResult<DbDataReader>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<DbDataReader>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<object>> CommandScalarExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutingAsync(command, eventData, default(InterceptionResult<object>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<object>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<int>> CommandNonQueryExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutingAsync(command, eventData, default(InterceptionResult<int>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<int>>);
	}

	private CommandEventData BroadcastCommandExecuting(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEventData commandEventData = new CommandEventData((EventDefinitionBase)(object)definition, CommandExecuting, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEventData;
		static string CommandExecuting(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, CommandType, int, string, string> obj = (EventDefinition<string, CommandType, int, string, string>)(object)val;
			CommandEventData commandEventData2 = (CommandEventData)(object)payload;
			return obj.GenerateMessage(commandEventData2.Command.Parameters.FormatParameters(commandEventData2.LogParameterValues), commandEventData2.Command.CommandType, commandEventData2.Command.CommandTimeout, Environment.NewLine, commandEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbDataReader CommandReaderExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual object? CommandScalarExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual int CommandNonQueryExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<DbDataReader> CommandReaderExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<object?> CommandScalarExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<int> CommandNonQueryExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	private CommandExecutedEventData BroadcastCommandExecuted(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, object methodResult, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandExecutedEventData commandExecutedEventData = new CommandExecutedEventData((EventDefinitionBase)(object)definition, CommandExecuted, connection, command, logCommandText, context, executeMethod, commandId, connectionId, methodResult, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandExecutedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandExecutedEventData;
		static string CommandExecuted(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandExecutedEventData commandExecutedEventData2 = (CommandExecutedEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandExecutedEventData2.Duration.TotalMilliseconds), commandExecutedEventData2.Command.Parameters.FormatParameters(commandExecutedEventData2.LogParameterValues), commandExecutedEventData2.Command.CommandType, commandExecutedEventData2.Command.CommandTimeout, Environment.NewLine, commandExecutedEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual void CommandError(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandFailed(command, eventData);
		}
	}

	private void LogCommandError(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual Task CommandErrorAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandFailedAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private CommandErrorEventData BroadcastCommandError(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandErrorEventData commandErrorEventData = new CommandErrorEventData((EventDefinitionBase)(object)definition, CommandError, connection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandErrorEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandErrorEventData;
		static string CommandError(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandErrorEventData commandErrorEventData2 = (CommandErrorEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandErrorEventData2.Duration.TotalMilliseconds), commandErrorEventData2.Command.Parameters.FormatParameters(commandErrorEventData2.LogParameterValues), commandErrorEventData2.Command.CommandType, commandErrorEventData2.Command.CommandTimeout, Environment.NewLine, commandErrorEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <inheritdoc />
	public virtual void CommandCanceled(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandCanceled(command, eventData);
		}
	}

	/// <inheritdoc />
	public virtual Task CommandCanceledAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCanceledAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private void LogCommandCanceled(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	private CommandEndEventData BroadcastCommandCanceled(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCanceled, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCanceled(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandEndEventData2.Duration.TotalMilliseconds), commandEndEventData2.Command.Parameters.FormatParameters(commandEndEventData2.LogParameterValues), commandEndEventData2.Command.CommandType, commandEndEventData2.Command.CommandTimeout, Environment.NewLine, commandEndEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderClosing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: false, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosing(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult> DataReaderClosingAsync(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: true, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosingAsync(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(ValueTask<InterceptionResult>);
	}

	private static string CreateDataReaderClosingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string> obj = (EventDefinition<string, string>)(object)definition;
		DataReaderClosingEventData dataReaderClosingEventData = (DataReaderClosingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderClosingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderClosingEventData.Command.Connection?.DataSource ?? "<Unknown>");
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderDisposing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime, TimeSpan duration)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderDisposingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string, int> val = RelationalResources.LogDisposingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource, (int)duration.TotalMilliseconds, (Exception)null);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			DataReaderDisposingEventData dataReaderDisposingEventData = new DataReaderDisposingEventData((EventDefinitionBase)(object)val, CreateDataReaderDisposingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, recordsAffected, readCount, startTime, duration);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderDisposingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderDisposing(command, dataReaderDisposingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	private static string CreateDataReaderDisposingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string, int> obj = (EventDefinition<string, string, int>)(object)definition;
		DataReaderDisposingEventData dataReaderDisposingEventData = (DataReaderDisposingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderDisposingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderDisposingEventData.Command.Connection?.DataSource ?? "<Unknown>", (int)dataReaderDisposingEventData.Duration.TotalMilliseconds, (Exception)null);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandCreate(DateTimeOffset now)
	{
		return now > _suppressCommandCreateExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandExecute(DateTimeOffset now)
	{
		return now > _suppressCommandExecuteExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderClose(DateTimeOffset now)
	{
		return now > _suppressDataReaderClosingExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderDispose(DateTimeOffset now)
	{
		return now > _suppressDataReaderDisposingExpiration;
	}
}
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Diagnostics.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class RelationalCommandDiagnosticsLogger : DiagnosticsLogger<Command>, IRelationalCommandDiagnosticsLogger, IDiagnosticsLogger<Command>, IDiagnosticsLogger
{
	private DateTimeOffset _suppressCommandCreateExpiration;

	private DateTimeOffset _suppressCommandExecuteExpiration;

	private DateTimeOffset _suppressDataReaderClosingExpiration;

	private DateTimeOffset _suppressDataReaderDisposingExpiration;

	private readonly TimeSpan _loggingCacheTime;

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public RelationalCommandDiagnosticsLogger(ILoggerFactory loggerFactory, ILoggingOptions loggingOptions, DiagnosticSource diagnosticSource, LoggingDefinitions loggingDefinitions, IDbContextLogger contextLogger, IDbContextOptions contextOptions, IInterceptors? interceptors = null)
		: base(loggerFactory, loggingOptions, diagnosticSource, loggingDefinitions, contextLogger, interceptors)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		CoreOptionsExtension val = (CoreOptionsExtension)(((object)contextOptions.FindExtension<CoreOptionsExtension>()) ?? ((object)new CoreOptionsExtension()));
		_loggingCacheTime = val.LoggingCacheTime;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbCommand> CommandCreating(IRelationalConnection connection, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandCreateExpiration = startTime + _loggingCacheTime;
		EventDefinition<string> val = RelationalResources.LogCommandCreating((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandCorrelatedEventData eventData = BroadcastCommandCreating(connection.DbConnection, context, commandMethod, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreating(eventData, default(InterceptionResult<DbCommand>));
			}
		}
		return default(InterceptionResult<DbCommand>);
	}

	private CommandCorrelatedEventData BroadcastCommandCreating(DbConnection connection, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandCorrelatedEventData commandCorrelatedEventData = new CommandCorrelatedEventData((EventDefinitionBase)(object)definition, CommandCreating, connection, context, executeMethod, commandId, connectionId, async, startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandCorrelatedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandCorrelatedEventData;
		static string CommandCreating(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string> obj = (EventDefinition<string>)(object)val;
			CommandCorrelatedEventData commandCorrelatedEventData2 = (CommandCorrelatedEventData)(object)payload;
			return obj.GenerateMessage(commandCorrelatedEventData2.ExecuteMethod.ToString());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandCreated(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandCreated((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandCreated(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreated(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandCreated(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCreated, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCreated(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandInitialized(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandInitialized((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandInitialized(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandInitialized(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandInitialized(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandInitialized, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandInitialized(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbDataReader> CommandReaderExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuting(command, eventData, default(InterceptionResult<DbDataReader>));
			}
		}
		return default(InterceptionResult<DbDataReader>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<object> CommandScalarExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuting(command, eventData, default(InterceptionResult<object>));
			}
		}
		return default(InterceptionResult<object>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<int> CommandNonQueryExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuting(command, eventData, default(InterceptionResult<int>));
			}
		}
		return default(InterceptionResult<int>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<DbDataReader>> CommandReaderExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutingAsync(command, eventData, default(InterceptionResult<DbDataReader>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<DbDataReader>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<object>> CommandScalarExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutingAsync(command, eventData, default(InterceptionResult<object>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<object>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<int>> CommandNonQueryExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutingAsync(command, eventData, default(InterceptionResult<int>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<int>>);
	}

	private CommandEventData BroadcastCommandExecuting(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEventData commandEventData = new CommandEventData((EventDefinitionBase)(object)definition, CommandExecuting, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEventData;
		static string CommandExecuting(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, CommandType, int, string, string> obj = (EventDefinition<string, CommandType, int, string, string>)(object)val;
			CommandEventData commandEventData2 = (CommandEventData)(object)payload;
			return obj.GenerateMessage(commandEventData2.Command.Parameters.FormatParameters(commandEventData2.LogParameterValues), commandEventData2.Command.CommandType, commandEventData2.Command.CommandTimeout, Environment.NewLine, commandEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbDataReader CommandReaderExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual object? CommandScalarExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual int CommandNonQueryExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<DbDataReader> CommandReaderExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<object?> CommandScalarExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<int> CommandNonQueryExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	private CommandExecutedEventData BroadcastCommandExecuted(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, object methodResult, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandExecutedEventData commandExecutedEventData = new CommandExecutedEventData((EventDefinitionBase)(object)definition, CommandExecuted, connection, command, logCommandText, context, executeMethod, commandId, connectionId, methodResult, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandExecutedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandExecutedEventData;
		static string CommandExecuted(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandExecutedEventData commandExecutedEventData2 = (CommandExecutedEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandExecutedEventData2.Duration.TotalMilliseconds), commandExecutedEventData2.Command.Parameters.FormatParameters(commandExecutedEventData2.LogParameterValues), commandExecutedEventData2.Command.CommandType, commandExecutedEventData2.Command.CommandTimeout, Environment.NewLine, commandExecutedEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual void CommandError(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandFailed(command, eventData);
		}
	}

	private void LogCommandError(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual Task CommandErrorAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandFailedAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private CommandErrorEventData BroadcastCommandError(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandErrorEventData commandErrorEventData = new CommandErrorEventData((EventDefinitionBase)(object)definition, CommandError, connection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandErrorEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandErrorEventData;
		static string CommandError(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandErrorEventData commandErrorEventData2 = (CommandErrorEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandErrorEventData2.Duration.TotalMilliseconds), commandErrorEventData2.Command.Parameters.FormatParameters(commandErrorEventData2.LogParameterValues), commandErrorEventData2.Command.CommandType, commandErrorEventData2.Command.CommandTimeout, Environment.NewLine, commandErrorEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <inheritdoc />
	public virtual void CommandCanceled(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandCanceled(command, eventData);
		}
	}

	/// <inheritdoc />
	public virtual Task CommandCanceledAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCanceledAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private void LogCommandCanceled(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	private CommandEndEventData BroadcastCommandCanceled(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCanceled, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCanceled(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandEndEventData2.Duration.TotalMilliseconds), commandEndEventData2.Command.Parameters.FormatParameters(commandEndEventData2.LogParameterValues), commandEndEventData2.Command.CommandType, commandEndEventData2.Command.CommandTimeout, Environment.NewLine, commandEndEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderClosing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: false, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosing(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult> DataReaderClosingAsync(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: true, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosingAsync(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(ValueTask<InterceptionResult>);
	}

	private static string CreateDataReaderClosingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string> obj = (EventDefinition<string, string>)(object)definition;
		DataReaderClosingEventData dataReaderClosingEventData = (DataReaderClosingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderClosingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderClosingEventData.Command.Connection?.DataSource ?? "<Unknown>");
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderDisposing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime, TimeSpan duration)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderDisposingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string, int> val = RelationalResources.LogDisposingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource, (int)duration.TotalMilliseconds, (Exception)null);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			DataReaderDisposingEventData dataReaderDisposingEventData = new DataReaderDisposingEventData((EventDefinitionBase)(object)val, CreateDataReaderDisposingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, recordsAffected, readCount, startTime, duration);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderDisposingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderDisposing(command, dataReaderDisposingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	private static string CreateDataReaderDisposingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string, int> obj = (EventDefinition<string, string, int>)(object)definition;
		DataReaderDisposingEventData dataReaderDisposingEventData = (DataReaderDisposingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderDisposingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderDisposingEventData.Command.Connection?.DataSource ?? "<Unknown>", (int)dataReaderDisposingEventData.Duration.TotalMilliseconds, (Exception)null);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandCreate(DateTimeOffset now)
	{
		return now > _suppressCommandCreateExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandExecute(DateTimeOffset now)
	{
		return now > _suppressCommandExecuteExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderClose(DateTimeOffset now)
	{
		return now > _suppressDataReaderClosingExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderDispose(DateTimeOffset now)
	{
		return now > _suppressDataReaderDisposingExpiration;
	}
}
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Diagnostics.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class RelationalCommandDiagnosticsLogger : DiagnosticsLogger<Command>, IRelationalCommandDiagnosticsLogger, IDiagnosticsLogger<Command>, IDiagnosticsLogger
{
	private DateTimeOffset _suppressCommandCreateExpiration;

	private DateTimeOffset _suppressCommandExecuteExpiration;

	private DateTimeOffset _suppressDataReaderClosingExpiration;

	private DateTimeOffset _suppressDataReaderDisposingExpiration;

	private readonly TimeSpan _loggingCacheTime;

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public RelationalCommandDiagnosticsLogger(ILoggerFactory loggerFactory, ILoggingOptions loggingOptions, DiagnosticSource diagnosticSource, LoggingDefinitions loggingDefinitions, IDbContextLogger contextLogger, IDbContextOptions contextOptions, IInterceptors? interceptors = null)
		: base(loggerFactory, loggingOptions, diagnosticSource, loggingDefinitions, contextLogger, interceptors)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		CoreOptionsExtension val = (CoreOptionsExtension)(((object)contextOptions.FindExtension<CoreOptionsExtension>()) ?? ((object)new CoreOptionsExtension()));
		_loggingCacheTime = val.LoggingCacheTime;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbCommand> CommandCreating(IRelationalConnection connection, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandCreateExpiration = startTime + _loggingCacheTime;
		EventDefinition<string> val = RelationalResources.LogCommandCreating((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandCorrelatedEventData eventData = BroadcastCommandCreating(connection.DbConnection, context, commandMethod, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreating(eventData, default(InterceptionResult<DbCommand>));
			}
		}
		return default(InterceptionResult<DbCommand>);
	}

	private CommandCorrelatedEventData BroadcastCommandCreating(DbConnection connection, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandCorrelatedEventData commandCorrelatedEventData = new CommandCorrelatedEventData((EventDefinitionBase)(object)definition, CommandCreating, connection, context, executeMethod, commandId, connectionId, async, startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandCorrelatedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandCorrelatedEventData;
		static string CommandCreating(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string> obj = (EventDefinition<string>)(object)val;
			CommandCorrelatedEventData commandCorrelatedEventData2 = (CommandCorrelatedEventData)(object)payload;
			return obj.GenerateMessage(commandCorrelatedEventData2.ExecuteMethod.ToString());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandCreated(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandCreated((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandCreated(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreated(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandCreated(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCreated, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCreated(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandInitialized(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandInitialized((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandInitialized(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandInitialized(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandInitialized(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandInitialized, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandInitialized(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbDataReader> CommandReaderExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuting(command, eventData, default(InterceptionResult<DbDataReader>));
			}
		}
		return default(InterceptionResult<DbDataReader>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<object> CommandScalarExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuting(command, eventData, default(InterceptionResult<object>));
			}
		}
		return default(InterceptionResult<object>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<int> CommandNonQueryExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuting(command, eventData, default(InterceptionResult<int>));
			}
		}
		return default(InterceptionResult<int>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<DbDataReader>> CommandReaderExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutingAsync(command, eventData, default(InterceptionResult<DbDataReader>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<DbDataReader>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<object>> CommandScalarExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutingAsync(command, eventData, default(InterceptionResult<object>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<object>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<int>> CommandNonQueryExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutingAsync(command, eventData, default(InterceptionResult<int>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<int>>);
	}

	private CommandEventData BroadcastCommandExecuting(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEventData commandEventData = new CommandEventData((EventDefinitionBase)(object)definition, CommandExecuting, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEventData;
		static string CommandExecuting(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, CommandType, int, string, string> obj = (EventDefinition<string, CommandType, int, string, string>)(object)val;
			CommandEventData commandEventData2 = (CommandEventData)(object)payload;
			return obj.GenerateMessage(commandEventData2.Command.Parameters.FormatParameters(commandEventData2.LogParameterValues), commandEventData2.Command.CommandType, commandEventData2.Command.CommandTimeout, Environment.NewLine, commandEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbDataReader CommandReaderExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual object? CommandScalarExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual int CommandNonQueryExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<DbDataReader> CommandReaderExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<object?> CommandScalarExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<int> CommandNonQueryExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	private CommandExecutedEventData BroadcastCommandExecuted(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, object methodResult, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandExecutedEventData commandExecutedEventData = new CommandExecutedEventData((EventDefinitionBase)(object)definition, CommandExecuted, connection, command, logCommandText, context, executeMethod, commandId, connectionId, methodResult, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandExecutedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandExecutedEventData;
		static string CommandExecuted(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandExecutedEventData commandExecutedEventData2 = (CommandExecutedEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandExecutedEventData2.Duration.TotalMilliseconds), commandExecutedEventData2.Command.Parameters.FormatParameters(commandExecutedEventData2.LogParameterValues), commandExecutedEventData2.Command.CommandType, commandExecutedEventData2.Command.CommandTimeout, Environment.NewLine, commandExecutedEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual void CommandError(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandFailed(command, eventData);
		}
	}

	private void LogCommandError(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual Task CommandErrorAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandFailedAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private CommandErrorEventData BroadcastCommandError(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandErrorEventData commandErrorEventData = new CommandErrorEventData((EventDefinitionBase)(object)definition, CommandError, connection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandErrorEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandErrorEventData;
		static string CommandError(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandErrorEventData commandErrorEventData2 = (CommandErrorEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandErrorEventData2.Duration.TotalMilliseconds), commandErrorEventData2.Command.Parameters.FormatParameters(commandErrorEventData2.LogParameterValues), commandErrorEventData2.Command.CommandType, commandErrorEventData2.Command.CommandTimeout, Environment.NewLine, commandErrorEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <inheritdoc />
	public virtual void CommandCanceled(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandCanceled(command, eventData);
		}
	}

	/// <inheritdoc />
	public virtual Task CommandCanceledAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCanceledAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private void LogCommandCanceled(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	private CommandEndEventData BroadcastCommandCanceled(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCanceled, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCanceled(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandEndEventData2.Duration.TotalMilliseconds), commandEndEventData2.Command.Parameters.FormatParameters(commandEndEventData2.LogParameterValues), commandEndEventData2.Command.CommandType, commandEndEventData2.Command.CommandTimeout, Environment.NewLine, commandEndEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderClosing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: false, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosing(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult> DataReaderClosingAsync(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: true, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosingAsync(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(ValueTask<InterceptionResult>);
	}

	private static string CreateDataReaderClosingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string> obj = (EventDefinition<string, string>)(object)definition;
		DataReaderClosingEventData dataReaderClosingEventData = (DataReaderClosingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderClosingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderClosingEventData.Command.Connection?.DataSource ?? "<Unknown>");
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderDisposing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime, TimeSpan duration)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderDisposingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string, int> val = RelationalResources.LogDisposingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource, (int)duration.TotalMilliseconds, (Exception)null);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			DataReaderDisposingEventData dataReaderDisposingEventData = new DataReaderDisposingEventData((EventDefinitionBase)(object)val, CreateDataReaderDisposingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, recordsAffected, readCount, startTime, duration);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderDisposingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderDisposing(command, dataReaderDisposingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	private static string CreateDataReaderDisposingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string, int> obj = (EventDefinition<string, string, int>)(object)definition;
		DataReaderDisposingEventData dataReaderDisposingEventData = (DataReaderDisposingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderDisposingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderDisposingEventData.Command.Connection?.DataSource ?? "<Unknown>", (int)dataReaderDisposingEventData.Duration.TotalMilliseconds, (Exception)null);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandCreate(DateTimeOffset now)
	{
		return now > _suppressCommandCreateExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandExecute(DateTimeOffset now)
	{
		return now > _suppressCommandExecuteExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderClose(DateTimeOffset now)
	{
		return now > _suppressDataReaderClosingExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderDispose(DateTimeOffset now)
	{
		return now > _suppressDataReaderDisposingExpiration;
	}
}
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Diagnostics.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class RelationalCommandDiagnosticsLogger : DiagnosticsLogger<Command>, IRelationalCommandDiagnosticsLogger, IDiagnosticsLogger<Command>, IDiagnosticsLogger
{
	private DateTimeOffset _suppressCommandCreateExpiration;

	private DateTimeOffset _suppressCommandExecuteExpiration;

	private DateTimeOffset _suppressDataReaderClosingExpiration;

	private DateTimeOffset _suppressDataReaderDisposingExpiration;

	private readonly TimeSpan _loggingCacheTime;

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public RelationalCommandDiagnosticsLogger(ILoggerFactory loggerFactory, ILoggingOptions loggingOptions, DiagnosticSource diagnosticSource, LoggingDefinitions loggingDefinitions, IDbContextLogger contextLogger, IDbContextOptions contextOptions, IInterceptors? interceptors = null)
		: base(loggerFactory, loggingOptions, diagnosticSource, loggingDefinitions, contextLogger, interceptors)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		CoreOptionsExtension val = (CoreOptionsExtension)(((object)contextOptions.FindExtension<CoreOptionsExtension>()) ?? ((object)new CoreOptionsExtension()));
		_loggingCacheTime = val.LoggingCacheTime;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbCommand> CommandCreating(IRelationalConnection connection, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandCreateExpiration = startTime + _loggingCacheTime;
		EventDefinition<string> val = RelationalResources.LogCommandCreating((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandCorrelatedEventData eventData = BroadcastCommandCreating(connection.DbConnection, context, commandMethod, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreating(eventData, default(InterceptionResult<DbCommand>));
			}
		}
		return default(InterceptionResult<DbCommand>);
	}

	private CommandCorrelatedEventData BroadcastCommandCreating(DbConnection connection, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandCorrelatedEventData commandCorrelatedEventData = new CommandCorrelatedEventData((EventDefinitionBase)(object)definition, CommandCreating, connection, context, executeMethod, commandId, connectionId, async, startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandCorrelatedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandCorrelatedEventData;
		static string CommandCreating(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string> obj = (EventDefinition<string>)(object)val;
			CommandCorrelatedEventData commandCorrelatedEventData2 = (CommandCorrelatedEventData)(object)payload;
			return obj.GenerateMessage(commandCorrelatedEventData2.ExecuteMethod.ToString());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandCreated(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandCreated((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandCreated(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCreated(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandCreated(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCreated, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCreated(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbCommand CommandInitialized(IRelationalConnection connection, DbCommand command, DbCommandMethod commandMethod, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, int> val = RelationalResources.LogCommandInitialized((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, commandMethod.ToString(), (int)duration.TotalMilliseconds);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandCreateExpiration = default(DateTimeOffset);
			CommandEndEventData eventData = BroadcastCommandInitialized(connection.DbConnection, command, context, commandMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandInitialized(eventData, command);
			}
		}
		return command;
	}

	private CommandEndEventData BroadcastCommandInitialized(DbConnection connection, DbCommand command, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, int> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandInitialized, connection, command, command.CommandText, context, executeMethod, commandId, connectionId, async, logParameterValues: false, startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandInitialized(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, int> obj = (EventDefinition<string, int>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(commandEndEventData2.ExecuteMethod.ToString(), (int)commandEndEventData2.Duration.TotalMilliseconds);
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<DbDataReader> CommandReaderExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuting(command, eventData, default(InterceptionResult<DbDataReader>));
			}
		}
		return default(InterceptionResult<DbDataReader>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<object> CommandScalarExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuting(command, eventData, default(InterceptionResult<object>));
			}
		}
		return default(InterceptionResult<object>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult<int> CommandNonQueryExecuting(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: false, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuting(command, eventData, default(InterceptionResult<int>));
			}
		}
		return default(InterceptionResult<int>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<DbDataReader>> CommandReaderExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutingAsync(command, eventData, default(InterceptionResult<DbDataReader>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<DbDataReader>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<object>> CommandScalarExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutingAsync(command, eventData, default(InterceptionResult<object>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<object>>);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult<int>> CommandNonQueryExecutingAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DateTimeOffset startTime, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		_suppressCommandExecuteExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, CommandType, int, string, string> val = RelationalResources.LogExecutingCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandEventData eventData = BroadcastCommandExecuting(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, async: true, startTime, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutingAsync(command, eventData, default(InterceptionResult<int>), cancellationToken);
			}
		}
		return default(ValueTask<InterceptionResult<int>>);
	}

	private CommandEventData BroadcastCommandExecuting(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, EventDefinition<string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEventData commandEventData = new CommandEventData((EventDefinitionBase)(object)definition, CommandExecuting, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEventData;
		static string CommandExecuting(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, CommandType, int, string, string> obj = (EventDefinition<string, CommandType, int, string, string>)(object)val;
			CommandEventData commandEventData2 = (CommandEventData)(object)payload;
			return obj.GenerateMessage(commandEventData2.Command.Parameters.FormatParameters(commandEventData2.LogParameterValues), commandEventData2.Command.CommandType, commandEventData2.Command.CommandTimeout, Environment.NewLine, commandEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual DbDataReader CommandReaderExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual object? CommandScalarExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual int CommandNonQueryExecuted(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecuted(command, eventData, methodResult);
			}
		}
		return methodResult;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<DbDataReader> CommandReaderExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, DbDataReader methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteReader, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ReaderExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<object?> CommandScalarExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, object? methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteScalar, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.ScalarExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<int> CommandNonQueryExecutedAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, Guid commandId, Guid connectionId, int methodResult, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogExecutedCommand((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			_suppressCommandExecuteExpiration = default(DateTimeOffset);
			CommandExecutedEventData eventData = BroadcastCommandExecuted(connection.DbConnection, command, logCommandText, context, DbCommandMethod.ExecuteNonQuery, commandId, connectionId, methodResult, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.NonQueryExecutedAsync(command, eventData, methodResult, cancellationToken);
			}
		}
		return ValueTask.FromResult(methodResult);
	}

	private CommandExecutedEventData BroadcastCommandExecuted(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, object methodResult, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandExecutedEventData commandExecutedEventData = new CommandExecutedEventData((EventDefinitionBase)(object)definition, CommandExecuted, connection, command, logCommandText, context, executeMethod, commandId, connectionId, methodResult, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandExecutedEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandExecutedEventData;
		static string CommandExecuted(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandExecutedEventData commandExecutedEventData2 = (CommandExecutedEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandExecutedEventData2.Duration.TotalMilliseconds), commandExecutedEventData2.Command.Parameters.FormatParameters(commandExecutedEventData2.LogParameterValues), commandExecutedEventData2.Command.CommandType, commandExecutedEventData2.Command.CommandTimeout, Environment.NewLine, commandExecutedEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual void CommandError(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandFailed(command, eventData);
		}
	}

	private void LogCommandError(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual Task CommandErrorAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandFailed((IDiagnosticsLogger)(object)this);
		LogCommandError(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandErrorEventData eventData = BroadcastCommandError(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandFailedAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private CommandErrorEventData BroadcastCommandError(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandErrorEventData commandErrorEventData = new CommandErrorEventData((EventDefinitionBase)(object)definition, CommandError, connection, command, logCommandText, context, executeMethod, commandId, connectionId, exception, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandErrorEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandErrorEventData;
		static string CommandError(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandErrorEventData commandErrorEventData2 = (CommandErrorEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandErrorEventData2.Duration.TotalMilliseconds), commandErrorEventData2.Command.Parameters.FormatParameters(commandErrorEventData2.LogParameterValues), commandErrorEventData2.Command.CommandType, commandErrorEventData2.Command.CommandTimeout, Environment.NewLine, commandErrorEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <inheritdoc />
	public virtual void CommandCanceled(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource)
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: false, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			dbCommandInterceptor?.CommandCanceled(command, eventData);
		}
	}

	/// <inheritdoc />
	public virtual Task CommandCanceledAsync(IRelationalConnection connection, DbCommand command, string logCommandText, DbContext? context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, DateTimeOffset startTime, TimeSpan duration, CommandSource commandSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		EventDefinition<string, string, CommandType, int, string, string> val = RelationalResources.LogCommandCanceled((IDiagnosticsLogger)(object)this);
		LogCommandCanceled(command, logCommandText, duration, val);
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool diagnosticSourceEnabled = default(bool);
		bool simpleLogEnabled = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref diagnosticSourceEnabled, ref simpleLogEnabled))
		{
			CommandEndEventData eventData = BroadcastCommandCanceled(connection.DbConnection, command, logCommandText, context, executeMethod, commandId, connectionId, async: true, startTime, duration, val, diagnosticSourceEnabled, simpleLogEnabled, commandSource);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.CommandCanceledAsync(command, eventData, cancellationToken);
			}
		}
		return Task.CompletedTask;
	}

	private void LogCommandCanceled(DbCommand command, string logCommandText, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition)
	{
		if (base.ShouldLog((EventDefinitionBase)(object)definition))
		{
			definition.Log<Command>((IDiagnosticsLogger<Command>)(object)this, string.Format(CultureInfo.InvariantCulture, "{0:N0}", duration.TotalMilliseconds), command.Parameters.FormatParameters(((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData()), command.CommandType, command.CommandTimeout, Environment.NewLine, logCommandText.TrimEnd());
		}
	}

	private CommandEndEventData BroadcastCommandCanceled(DbConnection connection, DbCommand command, string logCommandText, DbContext context, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime, TimeSpan duration, EventDefinition<string, string, CommandType, int, string, string> definition, bool diagnosticSourceEnabled, bool simpleLogEnabled, CommandSource commandSource)
	{
		CommandEndEventData commandEndEventData = new CommandEndEventData((EventDefinitionBase)(object)definition, CommandCanceled, connection, command, logCommandText, context, executeMethod, commandId, connectionId, async, ((DiagnosticsLogger<Command>)(object)this).ShouldLogSensitiveData(), startTime, duration, commandSource);
		base.DispatchEventData((EventDefinitionBase)(object)definition, (EventData)(object)commandEndEventData, diagnosticSourceEnabled, simpleLogEnabled);
		return commandEndEventData;
		static string CommandCanceled(EventDefinitionBase val, EventData payload)
		{
			EventDefinition<string, string, CommandType, int, string, string> obj = (EventDefinition<string, string, CommandType, int, string, string>)(object)val;
			CommandEndEventData commandEndEventData2 = (CommandEndEventData)(object)payload;
			return obj.GenerateMessage(string.Format(CultureInfo.InvariantCulture, "{0:N0}", commandEndEventData2.Duration.TotalMilliseconds), commandEndEventData2.Command.Parameters.FormatParameters(commandEndEventData2.LogParameterValues), commandEndEventData2.Command.CommandType, commandEndEventData2.Command.CommandTimeout, Environment.NewLine, commandEndEventData2.LogCommandText.TrimEnd());
		}
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderClosing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: false, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosing(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual ValueTask<InterceptionResult> DataReaderClosingAsync(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderClosingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string> val = RelationalResources.LogClosingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderClosingExpiration = default(DateTimeOffset);
			DataReaderClosingEventData dataReaderClosingEventData = new DataReaderClosingEventData((EventDefinitionBase)(object)val, CreateDataReaderClosingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, async: true, recordsAffected, readCount, startTime);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderClosingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderClosingAsync(command, dataReaderClosingEventData, default(InterceptionResult));
			}
		}
		return default(ValueTask<InterceptionResult>);
	}

	private static string CreateDataReaderClosingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string> obj = (EventDefinition<string, string>)(object)definition;
		DataReaderClosingEventData dataReaderClosingEventData = (DataReaderClosingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderClosingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderClosingEventData.Command.Connection?.DataSource ?? "<Unknown>");
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual InterceptionResult DataReaderDisposing(IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime, TimeSpan duration)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		_suppressDataReaderDisposingExpiration = startTime + _loggingCacheTime;
		EventDefinition<string, string, int> val = RelationalResources.LogDisposingDataReader((IDiagnosticsLogger)(object)this);
		if (base.ShouldLog((EventDefinitionBase)(object)val))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			val.Log<Command>((IDiagnosticsLogger<Command>)(object)this, connection.DbConnection.Database, connection.DbConnection.DataSource, (int)duration.TotalMilliseconds, (Exception)null);
		}
		IDbCommandInterceptor dbCommandInterceptor = default(IDbCommandInterceptor);
		bool flag = default(bool);
		bool flag2 = default(bool);
		if (base.NeedsEventData<IDbCommandInterceptor>((EventDefinitionBase)(object)val, ref dbCommandInterceptor, ref flag, ref flag2))
		{
			_suppressDataReaderDisposingExpiration = default(DateTimeOffset);
			DataReaderDisposingEventData dataReaderDisposingEventData = new DataReaderDisposingEventData((EventDefinitionBase)(object)val, CreateDataReaderDisposingString, command, dataReader, connection.Context, commandId, connection.ConnectionId, recordsAffected, readCount, startTime, duration);
			base.DispatchEventData((EventDefinitionBase)(object)val, (EventData)(object)dataReaderDisposingEventData, flag, flag2);
			if (dbCommandInterceptor != null)
			{
				return dbCommandInterceptor.DataReaderDisposing(command, dataReaderDisposingEventData, default(InterceptionResult));
			}
		}
		return default(InterceptionResult);
	}

	private static string CreateDataReaderDisposingString(EventDefinitionBase definition, EventData payload)
	{
		EventDefinition<string, string, int> obj = (EventDefinition<string, string, int>)(object)definition;
		DataReaderDisposingEventData dataReaderDisposingEventData = (DataReaderDisposingEventData)(object)payload;
		return obj.GenerateMessage(dataReaderDisposingEventData.Command.Connection?.Database ?? "<Unknown>", dataReaderDisposingEventData.Command.Connection?.DataSource ?? "<Unknown>", (int)dataReaderDisposingEventData.Duration.TotalMilliseconds, (Exception)null);
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandCreate(DateTimeOffset now)
	{
		return now > _suppressCommandCreateExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogCommandExecute(DateTimeOffset now)
	{
		return now > _suppressCommandExecuteExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderClose(DateTimeOffset now)
	{
		return now > _suppressDataReaderClosingExpiration;
	}

	/// <summary>
	///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
	///     the same compatibility standards as public APIs. It may be changed or removed without notice in
	///     any release. You should only use it directly in your code with extreme caution and knowing that
	///     doing so can result in application failures when updating to a new Entity Framework Core release.
	/// </summary>
	public virtual bool ShouldLogDataReaderDispose(DateTimeOffset now)
	{
		return now > _suppressDataReaderDisposingExpiration;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
