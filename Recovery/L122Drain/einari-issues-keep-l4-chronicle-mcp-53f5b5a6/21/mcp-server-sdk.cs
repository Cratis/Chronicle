using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server;

/// <inheritdoc />
internal sealed class McpServerImpl : McpServer
{
	/// <summary>Tracks an active <c>subscriptions/listen</c> subscription for notification fan-out.</summary>
	/// <param name="Id">The id of the <c>subscriptions/listen</c> request, reused as the SEP-2575 subscription id.</param>
	/// <param name="Granted">The notification types the server agreed to deliver on this subscription.</param>
	/// <param name="RelatedTransport">
	/// The transport the <c>subscriptions/listen</c> request arrived on. For Streamable HTTP this is the
	/// per-request response stream the subscription must be delivered on; for stdio it is <see langword="null" />,
	/// so notifications fall back to the shared session channel.
	/// </param>
	private sealed record ActiveSubscription(RequestId Id, SubscriptionsListenNotifications Granted, ITransport? RelatedTransport);

	private readonly ILogger _logger;

	private readonly ITransport _sessionTransport;

	private readonly bool _servicesScopePerRequest;

	private readonly List<Action> _disposables = new List<Action>();

	private readonly NotificationHandlers _notificationHandlers;

	private readonly RequestHandlers _requestHandlers;

	private readonly McpSessionHandler _sessionHandler;

	private readonly string[] _supportedProtocolVersions;

	private readonly string[] _initializeHandshakeProtocolVersions;

	private readonly string[] _perRequestMetadataProtocolVersions;

	private readonly SemaphoreSlim _disposeLock = new SemaphoreSlim(1, 1);

	private readonly ConcurrentDictionary<string, MrtrContinuation> _mrtrContinuations = new ConcurrentDictionary<string, MrtrContinuation>();

	private readonly ConcurrentDictionary<RequestId, MrtrContext> _mrtrContextsByRequestId = new ConcurrentDictionary<RequestId, MrtrContext>();

	private static readonly string[] s_perRequestMetadataKeys = new string[4] { "io.modelcontextprotocol/protocolVersion", "io.modelcontextprotocol/clientInfo", "io.modelcontextprotocol/clientCapabilities", "io.modelcontextprotocol/logLevel" };

	private int _mrtrInFlightCount = 1;

	private readonly TaskCompletionSource<bool> _allMrtrHandlersCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private ClientCapabilities? _clientCapabilities;

	private Implementation? _clientInfo;

	private readonly string _serverOnlyEndpointName;

	private string? _negotiatedProtocolVersion;

	private string _endpointName;

	private int _started;

	private bool _disposed;

	/// <summary>Holds a boxed <see cref="P:ModelContextProtocol.Server.McpServerImpl.LoggingLevel" /> value for the server.</summary>
	/// <remarks>
	/// Initialized to non-null the first time SetLevel is used. This is stored as a strong box
	/// rather than a nullable to be able to manipulate it atomically.
	/// </remarks>
	private StrongBox<LoggingLevel>? _loggingLevel;

	private readonly ConcurrentDictionary<RequestId, ActiveSubscription> _activeSubscriptions = new ConcurrentDictionary<RequestId, ActiveSubscription>();

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, string, Exception?> __ToolCallErrorCallback = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1433779783, "ToolCallError"), "\"{ToolName}\" threw an unhandled exception.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, string, bool, Exception?> __ToolCallCompletedCallback = LoggerMessage.Define<string, bool>(LogLevel.Information, new EventId(2065726448, "ToolCallCompleted"), "\"{ToolName}\" completed. IsError = {IsError}.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, string, Exception?> __GetPromptErrorCallback = LoggerMessage.Define<string>(LogLevel.Error, new EventId(955764025, "GetPromptError"), "GetPrompt \"{PromptName}\" threw an unhandled exception.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, string, Exception?> __GetPromptCompletedCallback = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1256276250, "GetPromptCompleted"), "GetPrompt \"{PromptName}\" completed.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, string, Exception?> __ReadResourceErrorCallback = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1073205151, "ReadResourceError"), "ReadResource \"{ResourceUri}\" threw an unhandled exception.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, string, Exception?> __ReadResourceCompletedCallback = LoggerMessage.Define<string>(LogLevel.Information, new EventId(2070680514, "ReadResourceCompleted"), "ReadResource \"{ResourceUri}\" completed.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, int, Exception?> __MrtrContinuationsCancelledCallback = LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1335898509, "MrtrContinuationsCancelled"), "Cancelled {Count} pending MRTR continuation(s) during session disposal.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, Exception?> __MrtrHandlerErrorCallback = LoggerMessage.Define(LogLevel.Debug, new EventId(732164654, "MrtrHandlerError"), "An MRTR handler threw an unhandled exception.", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private static readonly Action<ILogger, string, string, Exception?> __SubscriptionNotificationFailedCallback = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(138312166, "SubscriptionNotificationFailed"), "Failed to deliver \"{NotificationMethod}\" to subscription \"{SubscriptionId}\".", new LogDefineOptions
	{
		SkipEnabledCheck = true
	});

	internal static Implementation DefaultImplementation { get; } = new Implementation
	{
		Name = (AssemblyNameHelper.DefaultAssemblyName.Name ?? "McpServer"),
		Version = (AssemblyNameHelper.DefaultAssemblyName.Version?.ToString() ?? "1.0.0")
	};

	/// <inheritdoc />
	public override string? SessionId => _sessionTransport.SessionId;

	/// <inheritdoc />
	public override string? NegotiatedProtocolVersion => _negotiatedProtocolVersion;

	/// <inheritdoc />
	public ServerCapabilities ServerCapabilities { get; }

	/// <inheritdoc />
	public override ClientCapabilities? ClientCapabilities => _clientCapabilities;

	/// <inheritdoc />
	public override Implementation? ClientInfo => _clientInfo;

	/// <inheritdoc />
	public override McpServerOptions ServerOptions { get; }

	/// <inheritdoc />
	public override IServiceProvider? Services { get; }

	/// <inheritdoc />
	[Obsolete("The Logging feature is deprecated as of specification version 2026-07-28 and may be removed in a future version. See SEP-2577 for more information.", DiagnosticId = "MCP9005", UrlFormat = "https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/list-of-diagnostics.md#obsolete-apis")]
	public override LoggingLevel? LoggingLevel => _loggingLevel?.Value;

	/// <inheritdoc />
	public override bool IsMrtrSupported
	{
		get
		{
			if (!ClientSupportsMrtr())
			{
				return HasStatefulTransport();
			}
			return true;
		}
	}

	/// <summary>
	/// Creates a new instance of <see cref="T:ModelContextProtocol.Server.McpServerImpl" />.
	/// </summary>
	/// <param name="transport">Transport to use for the server representing an already-established session.</param>
	/// <param name="options">Configuration options for this server, including capabilities.
	/// Make sure to accurately reflect exactly what capabilities the server supports and does not support.</param>
	/// <param name="loggerFactory">Logger factory to use for logging</param>
	/// <param name="serviceProvider">Optional service provider to use for dependency injection</param>
	/// <exception cref="T:ModelContextProtocol.McpException">The server was incorrectly configured.</exception>
	public McpServerImpl(ITransport transport, McpServerOptions options, ILoggerFactory? loggerFactory, IServiceProvider? serviceProvider)
	{
		Throw.IfNull(transport, "transport");
		Throw.IfNull(options, "options");
		_sessionTransport = transport;
		ServerOptions = options;
		Services = serviceProvider;
		_supportedProtocolVersions = GetConfiguredSupportedProtocolVersions(options.ProtocolVersion);
		_initializeHandshakeProtocolVersions = _supportedProtocolVersions.Where(McpProtocolVersions.SupportsInitializeHandshake).ToArray();
		_perRequestMetadataProtocolVersions = _supportedProtocolVersions.Where(McpProtocolVersions.RequiresPerRequestMetadata).ToArray();
		_serverOnlyEndpointName = $"Server ({options.ServerInfo?.Name ?? DefaultImplementation.Name} {options.ServerInfo?.Version ?? DefaultImplementation.Version})";
		_endpointName = _serverOnlyEndpointName;
		_servicesScopePerRequest = options.ScopeRequests;
		_logger = loggerFactory?.CreateLogger<McpServer>() ?? NullLogger<McpServer>.Instance;
		_clientInfo = options.KnownClientInfo;
		_clientCapabilities = options.KnownClientCapabilities;
		UpdateEndpointNameWithClientInfo();
		_notificationHandlers = new NotificationHandlers();
		_requestHandlers = new RequestHandlers();
		ServerCapabilities = new ServerCapabilities();
		ConfigureInitialize(options);
		ConfigureDiscover(options);
		ConfigureTools(options);
		ConfigurePrompts(options);
		ConfigureResources(options);
		ConfigureLogging(options);
		ConfigureCompletion(options);
		ConfigureSubscriptions(options);
		ConfigureExperimentalAndExtensions(options);
		ConfigureMrtr();
		ConfigureCustomRequestHandlers(options);
		IEnumerable<KeyValuePair<string, Func<JsonRpcNotification, CancellationToken, ValueTask>>> notificationHandlers = options.Handlers.NotificationHandlers;
		if (notificationHandlers != null)
		{
			_notificationHandlers.RegisterRange(notificationHandlers);
		}
		if (HasStatefulTransport())
		{
			Register<McpServerTool>(ServerOptions.ToolCollection, "notifications/tools/list_changed");
			Register<McpServerPrompt>(ServerOptions.PromptCollection, "notifications/prompts/list_changed");
			Register<McpServerResource>(ServerOptions.ResourceCollection, "notifications/resources/list_changed");
		}
		JsonRpcMessageFilter incomingMessageFilter = PrependMetaReadingFilter(BuildMessageFilterPipeline(options.Filters.Message.IncomingFilters));
		JsonRpcMessageFilter outgoingMessageFilter = PrependServerInfoFilter(BuildMessageFilterPipeline(options.Filters.Message.OutgoingFilters), options.ServerInfo ?? DefaultImplementation);
		_sessionHandler = new McpSessionHandler(isServer: true, _sessionTransport, _endpointName, _requestHandlers, _notificationHandlers, incomingMessageFilter, outgoingMessageFilter, _logger);
		void Register<TPrimitive>(McpServerPrimitiveCollection<TPrimitive>? collection, string notificationMethod) where TPrimitive : notnull, IMcpServerPrimitive
		{
			if (collection != null)
			{
				EventHandler changed = delegate
				{
					SendListChangedNotificationAsync(notificationMethod);
				};
				collection.Changed += changed;
				_disposables.Add(delegate
				{
					collection.Changed -= changed;
				});
			}
		}
	}

	/// <summary>
	/// Wraps <paramref name="inner" /> so that, for every JSON-RPC request, a built-in filter first
	/// synchronizes server-side state (<see cref="F:ModelContextProtocol.Server.McpServerImpl._negotiatedProtocolVersion" />, <see cref="F:ModelContextProtocol.Server.McpServerImpl._clientInfo" />)
	/// from the per-request <c>_meta</c> values projected onto <see cref="T:ModelContextProtocol.Protocol.JsonRpcMessageContext" /> and
	/// validates the per-request protocol version, before delegating to the user-supplied incoming filters.
	/// </summary>
	/// <remarks>
	/// Under the 2026-07-28 protocol revision (SEP-2575) there is no <c>initialize</c> handshake, so the protocol
	/// version and client capabilities MUST be populated per-request. Client info is optional. Per-request client
	/// capabilities and client info are consumed request-scoped by <see cref="T:ModelContextProtocol.Server.DestinationBoundMcpServer" /> and are
	/// not read from server-wide state by request handlers. The shared <see cref="F:ModelContextProtocol.Server.McpServerImpl._clientInfo" /> write below is
	/// best-effort and used only to derive the session endpoint name for logging/telemetry. For initialize-handshake
	/// clients the per-request values are absent and the built-in filter is a no-op (the values were captured during
	/// the initialize handler).
	/// </remarks>
	private JsonRpcMessageFilter PrependMetaReadingFilter(JsonRpcMessageFilter inner)
	{
		JsonRpcMessageFilter metaReadingFilter = (Func<JsonRpcMessage, CancellationToken, Task> next) => async delegate(JsonRpcMessage message, CancellationToken cancellationToken)
		{
			if (message is JsonRpcRequest { Method: "initialize" } jsonRpcRequest)
			{
				ValidateInitializeRequestBoundary(jsonRpcRequest);
			}
			else if (message is JsonRpcRequest jsonRpcRequest2)
			{
				JsonRpcMessageContext context = jsonRpcRequest2.Context;
				bool flag = false;
				bool flag2 = HasMetaKey(jsonRpcRequest2, "io.modelcontextprotocol/protocolVersion");
				string key;
				bool flag3 = TryGetPerRequestMetadataKey(jsonRpcRequest2, out key);
				string text = context?.ProtocolVersion;
				if (text != null)
				{
					bool num = _negotiatedProtocolVersion != null;
					if (num)
					{
						SetNegotiatedProtocolVersion(text);
					}
					if (!((ReadOnlySpan<string>)_supportedProtocolVersions).Contains(text))
					{
						string[] supported = ((flag2 && _perRequestMetadataProtocolVersions.Length != 0) ? _perRequestMetadataProtocolVersions : _supportedProtocolVersions);
						throw new UnsupportedProtocolVersionException(text, supported);
					}
					if (McpProtocolVersions.RequiresPerRequestMetadata(text))
					{
						ValidateRequiredPerRequestMetadata(text, flag2, context.ClientCapabilities != null);
					}
					else if (McpProtocolVersions.SupportsInitializeHandshake(text))
					{
						if (_negotiatedProtocolVersion == null && flag2)
						{
							throw new UnsupportedProtocolVersionException(text, _perRequestMetadataProtocolVersions, "Protocol version '" + text + "' requires the initialize handshake and cannot be selected through per-request metadata.");
						}
						if (flag3)
						{
							ThrowReservedPerRequestMetadata(text, key);
						}
					}
					if (!num)
					{
						SetNegotiatedProtocolVersion(text);
					}
				}
				else if (_negotiatedProtocolVersion == null)
				{
					if (jsonRpcRequest2.Method == "server/discover")
					{
						throw new McpProtocolException("The 'server/discover' request requires per-request metadata declaring a supported protocol version.", McpErrorCode.InvalidParams);
					}
					if (flag3)
					{
						ThrowReservedPerRequestMetadata(null, key);
					}
				}
				else if (McpProtocolVersions.SupportsInitializeHandshake(_negotiatedProtocolVersion) && flag3)
				{
					ThrowReservedPerRequestMetadata(_negotiatedProtocolVersion, key);
				}
				ValidateRequestMethodBoundary(jsonRpcRequest2);
				Implementation implementation = context?.ClientInfo;
				if (implementation != null && (_clientInfo == null || !string.Equals(_clientInfo.Name, implementation.Name, StringComparison.Ordinal) || !string.Equals(_clientInfo.Version, implementation.Version, StringComparison.Ordinal)))
				{
					_clientInfo = implementation;
					flag = true;
				}
				if (flag)
				{
					UpdateEndpointNameWithClientInfo();
					_sessionHandler.EndpointName = _endpointName;
				}
			}
			else if (message is JsonRpcNotification notification)
			{
				ValidateNotificationBoundary(notification);
			}
			await next(message, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		};
		return (Func<JsonRpcMessage, CancellationToken, Task> next) => metaReadingFilter(inner(next));
	}

	private static void ValidateRequiredPerRequestMetadata(string protocolVersion, bool hasProtocolVersionMeta, bool hasClientCapabilitiesMeta)
	{
		if (!hasProtocolVersionMeta)
		{
			ThrowMissingPerRequestMetadata(protocolVersion, "io.modelcontextprotocol/protocolVersion");
		}
		if (!hasClientCapabilitiesMeta)
		{
			ThrowMissingPerRequestMetadata(protocolVersion, "io.modelcontextprotocol/clientCapabilities");
		}
	}

	private static void ThrowMissingPerRequestMetadata(string protocolVersion, string key)
	{
		throw new McpProtocolException($"Requests using protocol version '{protocolVersion}' must include '_meta/{key}'.", McpErrorCode.InvalidParams);
	}

	private static void ThrowReservedPerRequestMetadata(string? requestedProtocolVersion, string key)
	{
		throw new McpProtocolException((requestedProtocolVersion == null) ? ("The reserved per-request metadata key '_meta/" + key + "' requires a protocol version that uses per-request metadata.") : $"The reserved per-request metadata key '_meta/{key}' is not valid with protocol version '{requestedProtocolVersion}'.", McpErrorCode.InvalidRequest);
	}

	private static bool TryGetPerRequestMetadataKey(JsonRpcRequest request, out string key)
	{
		string[] array = s_perRequestMetadataKeys;
		foreach (string text in array)
		{
			if (HasMetaKey(request, text))
			{
				key = text;
				return true;
			}
		}
		key = "";
		return false;
	}

	private static bool HasMetaKey(JsonRpcRequest request, string key)
	{
		if (request.Params is JsonObject jsonObject && jsonObject["_meta"] is JsonObject jsonObject2)
		{
			return jsonObject2.ContainsKey(key);
		}
		return false;
	}

	/// <summary>
	/// Adds the server identity to every successful result on per-request-metadata protocol revisions.
	/// The filter runs before application filters so they can inspect or intentionally remove the metadata.
	/// </summary>
	private JsonRpcMessageFilter PrependServerInfoFilter(JsonRpcMessageFilter inner, Implementation serverInfo)
	{
		JsonRpcMessageFilter serverInfoFilter = (Func<JsonRpcMessage, CancellationToken, Task> next) => async delegate(JsonRpcMessage message, CancellationToken cancellationToken)
		{
			if (message is JsonRpcResponse { Result: JsonObject result } && McpProtocolVersions.RequiresPerRequestMetadata(message.Context?.ProtocolVersion ?? _negotiatedProtocolVersion))
			{
				JsonObject jsonObject = result["_meta"] as JsonObject;
				if (jsonObject == null)
				{
					jsonObject = (JsonObject)(result["_meta"] = new JsonObject());
				}
				jsonObject["io.modelcontextprotocol/serverInfo"] = JsonSerializer.SerializeToNode(serverInfo, McpJsonUtilities.JsonContext.Default.Implementation);
			}
			await next(message, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		};
		return (Func<JsonRpcMessage, CancellationToken, Task> next) => serverInfoFilter(inner(next));
	}

	private void ValidateInitializeRequestBoundary(JsonRpcRequest request)
	{
		if (McpProtocolVersions.RequiresPerRequestMetadata(request.Context?.ProtocolVersion))
		{
			throw new McpProtocolException($"Method '{"initialize"}' is not available on protocol version '{request.Context?.ProtocolVersion}'. Use '{"server/discover"}' and per-request metadata instead.", McpErrorCode.MethodNotFound);
		}
		string text = request.Context?.ProtocolVersion;
		if (text != null && !McpProtocolVersions.SupportsInitializeHandshake(text))
		{
			throw new UnsupportedProtocolVersionException(text, _initializeHandshakeProtocolVersions, "Protocol version '" + text + "' is not available through the initialize handshake.");
		}
		if (TryGetPerRequestMetadataKey(request, out string key))
		{
			ThrowReservedPerRequestMetadata(TryGetStringParam(request, "protocolVersion"), key);
		}
	}

	private static string? TryGetStringParam(JsonRpcRequest request, string propertyName)
	{
		if (request.Params is JsonObject jsonObject && jsonObject[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value))
		{
			return value;
		}
		return null;
	}

	private static string[] GetConfiguredSupportedProtocolVersions(string? protocolVersion)
	{
		if (protocolVersion == null)
		{
			return McpProtocolVersions.SupportedProtocolVersions;
		}
		if (!McpProtocolVersions.IsSupportedProtocolVersion(protocolVersion))
		{
			throw new McpException("Unsupported server protocol version '" + protocolVersion + "'. Supported protocol versions: " + string.Join(", ", McpProtocolVersions.SupportedProtocolVersions) + ".");
		}
		return new string[1] { protocolVersion };
	}

	private void ValidateNotificationBoundary(JsonRpcNotification notification)
	{
		if (notification.Method == "notifications/initialized" && McpProtocolVersions.RequiresPerRequestMetadata(notification.Context?.ProtocolVersion ?? _negotiatedProtocolVersion))
		{
			throw new McpProtocolException("The notification 'notifications/initialized' is only valid after the initialize handshake.", McpErrorCode.InvalidRequest);
		}
	}

	private void ValidateRequestMethodBoundary(JsonRpcRequest request)
	{
		bool flag = IsJuly2026OrLaterProtocolRequest(request);
		bool flag2 = !flag;
		if (flag2)
		{
			string method = request.Method;
			bool flag3 = ((method == "subscriptions/listen" || method == "server/discover") ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			throw new McpProtocolException($"The method '{request.Method}' requires a newer protocol revision that supports per-request metadata; the negotiated protocol version is '{NegotiatedProtocolVersion ?? "(none)"}'.", McpErrorCode.MethodNotFound);
		}
		flag2 = flag;
		if (flag2)
		{
			bool flag3;
			switch (request.Method)
			{
			case "ping":
			case "logging/setLevel":
			case "resources/subscribe":
			case "resources/unsubscribe":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = flag3;
		}
		if (flag2)
		{
			string removedMethodReplacementHint = GetRemovedMethodReplacementHint(request.Method);
			throw new McpProtocolException($"The method '{request.Method}' is not available on protocol version '{request.Context?.ProtocolVersion ?? NegotiatedProtocolVersion}'." + ((removedMethodReplacementHint == null) ? "" : (" " + removedMethodReplacementHint)), McpErrorCode.MethodNotFound);
		}
	}

	/// <summary>
	/// Returns guidance on the per-request-metadata replacement for a method that SEP-2575 removed,
	/// or <see langword="null" /> when the method has no direct replacement. Surfaced in the
	/// <see cref="F:ModelContextProtocol.McpErrorCode.MethodNotFound" /> error so a client that still calls the legacy RPC
	/// (for example <c>resources/subscribe</c>) learns how to migrate.
	/// </summary>
	private static string? GetRemovedMethodReplacementHint(string method)
	{
		switch (method)
		{
		case "logging/setLevel":
			return "Use the per-request '_meta/io.modelcontextprotocol/logLevel' field instead.";
		case "resources/subscribe":
		case "resources/unsubscribe":
			return "Use 'subscriptions/listen' with 'resourceSubscriptions' instead.";
		default:
			return null;
		}
	}

	/// <summary>
	/// Records the negotiated MCP protocol version for the session. The version is established exactly
	/// once: the initial <see langword="null" />-to-value transition is allowed (and racing requests that
	/// select the same version are idempotent no-ops), but any later attempt to switch to a different
	/// version throws. A single session MUST NOT change protocol versions, so a conflicting per-request
	/// <c>_meta</c> protocol version (or <c>Mcp-Protocol-Version</c> header) is a client error rather than
	/// something we silently overwrite.
	/// </summary>
	private void SetNegotiatedProtocolVersion(string protocolVersion)
	{
		string text = Interlocked.CompareExchange(ref _negotiatedProtocolVersion, protocolVersion, null);
		if (text == null)
		{
			_sessionHandler.NegotiatedProtocolVersion = protocolVersion;
		}
		else if (!string.Equals(text, protocolVersion, StringComparison.Ordinal))
		{
			throw new McpProtocolException($"The negotiated protocol version cannot change within a session. The session negotiated '{text}', but a request specified '{protocolVersion}'.", McpErrorCode.InvalidRequest);
		}
	}

	/// <summary>
	/// Returns the <see cref="P:ModelContextProtocol.Server.McpServerImpl.ServerCapabilities" /> to advertise in a specific response, suppressing the
	/// <c>listChanged</c> flags the server has no way to honor.
	/// </summary>
	/// <param name="listenStreamCanDeliverListChanged">
	/// <see langword="true" /> when the client this response targets can receive <c>*/list_changed</c>
	/// notifications over a <c>subscriptions/listen</c> stream.
	/// </param>
	/// <remarks>
	/// A stateless HTTP server has no session-wide channel to push unsolicited <c>*/list_changed</c>
	/// notifications. It can only deliver them over a <c>subscriptions/listen</c> stream, which requires both
	/// a 2026-07-28+ client (so the request is reachable at all) and a custom
	/// <see cref="P:ModelContextProtocol.Server.McpServerHandlers.SubscriptionsListenHandler" /> to own that stream (the built-in stateless
	/// handler grants no notifications). When neither the transport is stateful nor that stream can carry
	/// them, the <c>listChanged</c> flags are dropped so the server never advertises a capability it cannot
	/// deliver. Everything else (for example <c>resources.subscribe</c>) is preserved.
	/// </remarks>
	private ServerCapabilities GetAdvertisedCapabilities(bool listenStreamCanDeliverListChanged)
	{
		if (HasStatefulTransport() || listenStreamCanDeliverListChanged)
		{
			return ServerCapabilities;
		}
		ServerCapabilities obj = new ServerCapabilities
		{
			Experimental = ServerCapabilities.Experimental,
			Logging = ServerCapabilities.Logging,
			Completions = ServerCapabilities.Completions,
			Extensions = ServerCapabilities.Extensions,
			Prompts = ((ServerCapabilities.Prompts == null) ? null : new PromptsCapability
			{
				ListChanged = null
			})
		};
		ResourcesCapability resources = ServerCapabilities.Resources;
		obj.Resources = ((resources != null) ? new ResourcesCapability
		{
			Subscribe = resources.Subscribe,
			ListChanged = null
		} : null);
		obj.Tools = ((ServerCapabilities.Tools == null) ? null : new ToolsCapability
		{
			ListChanged = null
		});
		return obj;
	}

	/// <inheritdoc />
	public override async Task RunAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (Interlocked.Exchange(ref _started, 1) != 0)
		{
			throw new InvalidOperationException("RunAsync must only be called once.");
		}
		try
		{
			await _sessionHandler.ProcessMessagesAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			await DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	/// <inheritdoc />
	public override Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _sessionHandler.SendRequestAsync(request, cancellationToken);
	}

	/// <inheritdoc />
	public override Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _sessionHandler.SendMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	public override IAsyncDisposable RegisterNotificationHandler(string method, Func<JsonRpcNotification, CancellationToken, ValueTask> handler)
	{
		return _sessionHandler.RegisterNotificationHandler(method, handler);
	}

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		using (await _disposeLock.LockAsync().ConfigureAwait(continueOnCapturedContext: false))
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_disposables.ForEach(delegate(Action d)
			{
				d();
			});
			await _sessionHandler.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			int count = _mrtrContinuations.Count;
			foreach (MrtrContinuation value in _mrtrContinuations.Values)
			{
				value.CancelHandler();
			}
			if (count > 0)
			{
				MrtrContinuationsCancelled(count);
			}
			if (Interlocked.Decrement(ref _mrtrInFlightCount) != 0)
			{
				await _allMrtrHandlersCompleted.Task.ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private void ConfigureInitialize(McpServerOptions options)
	{
		_requestHandlers.Set("initialize", async delegate(InitializeRequestParams request, JsonRpcRequest _, CancellationToken _)
		{
			_clientCapabilities = request?.Capabilities ?? new ClientCapabilities();
			_clientInfo = request?.ClientInfo;
			UpdateEndpointNameWithClientInfo();
			_sessionHandler.EndpointName = _endpointName;
			string text = options.ProtocolVersion;
			if (text != null)
			{
				string text2 = text;
				if (McpProtocolVersions.IsJuly2026OrLaterProtocolVersion(text2))
				{
					throw new UnsupportedProtocolVersionException(text2, _initializeHandshakeProtocolVersions, "Protocol version '" + text2 + "' is not available through the initialize handshake.");
				}
			}
			if (text == null)
			{
				string text3 = request?.ProtocolVersion;
				if (text3 != null)
				{
					if (McpProtocolVersions.IsJuly2026OrLaterProtocolVersion(text3))
					{
						throw new UnsupportedProtocolVersionException(text3, _initializeHandshakeProtocolVersions, "Protocol version '" + text3 + "' is not available through the initialize handshake.");
					}
					text = (McpProtocolVersions.SupportsInitializeHandshake(text3) ? text3 : "2025-11-25");
				}
				else
				{
					text = "2025-11-25";
				}
			}
			string text4 = text ?? "2025-11-25";
			_negotiatedProtocolVersion = text4;
			_sessionHandler.NegotiatedProtocolVersion = text4;
			return new InitializeResult
			{
				ProtocolVersion = text4,
				Instructions = options.ServerInstructions,
				ServerInfo = (options.ServerInfo ?? DefaultImplementation),
				Capabilities = GetAdvertisedCapabilities(listenStreamCanDeliverListChanged: false)
			};
		}, McpJsonUtilities.JsonContext.Default.InitializeRequestParams, McpJsonUtilities.JsonContext.Default.InitializeResult);
	}

	/// <summary>
	/// Registers the <c>server/discover</c> request handler introduced by the 2026-07-28 protocol revision (SEP-2575).
	/// </summary>
	/// <remarks>
	/// The handler is registered unconditionally so requests can be routed to the protocol boundary filters. Successful
	/// <c>server/discover</c> responses advertise only protocol versions available through per-request metadata; versions
	/// that require the <c>initialize</c> handshake are negotiated through <c>initialize</c> instead.
	/// </remarks>
	private void ConfigureDiscover(McpServerOptions options)
	{
		_requestHandlers.Set("server/discover", delegate
		{
			DiscoverResult discoverResult = new DiscoverResult();
			string[] perRequestMetadataProtocolVersions = _perRequestMetadataProtocolVersions;
			int num = perRequestMetadataProtocolVersions.Length;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(perRequestMetadataProtocolVersions);
			readOnlySpan.CopyTo(span.Slice(num2, readOnlySpan.Length));
			num2 += readOnlySpan.Length;
			discoverResult.SupportedVersions = list;
			discoverResult.Capabilities = GetAdvertisedCapabilities(options.Handlers.SubscriptionsListenHandler != null);
			discoverResult.Instructions = options.ServerInstructions;
			discoverResult.TimeToLive = TimeSpan.Zero;
			discoverResult.CacheScope = CacheScope.Private;
			discoverResult.ResultType = "complete";
			return new ValueTask<DiscoverResult>(discoverResult);
		}, McpJsonUtilities.JsonContext.Default.DiscoverRequestParams, McpJsonUtilities.JsonContext.Default.DiscoverResult);
	}

	/// <summary>
	/// Registers the <c>subscriptions/listen</c> request handler introduced by the 2026-07-28 protocol revision (SEP-2575).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The handler opens a long-lived response stream (over the per-request <see cref="T:ModelContextProtocol.Server.StreamableHttpPostTransport" />
	/// for HTTP, or the shared STDIO channel) that first sends
	/// <see cref="F:ModelContextProtocol.Protocol.NotificationMethods.SubscriptionsAcknowledgedNotification" /> reporting which subscriptions the
	/// server agreed to honor, and then streams matching notifications until the request is cancelled.
	/// </para>
	/// <para>
	/// Subscription-bound notifications carry the listen request's id in their
	/// <c>_meta/io.modelcontextprotocol/subscriptionId</c> field per SEP-2575 so clients can demultiplex.
	/// </para>
	/// <para>
	/// A server author may supply a custom <see cref="P:ModelContextProtocol.Server.McpServerHandlers.SubscriptionsListenHandler" /> to take
	/// over the stream entirely; see the design notes at the top of this method for the behavior.
	/// </para>
	/// </remarks>
	private void ConfigureSubscriptions(McpServerOptions options)
	{
		McpRequestHandler<SubscriptionsListenRequestParams, EmptyResult> subscriptionsListenHandler = options.Handlers.SubscriptionsListenHandler;
		if (subscriptionsListenHandler != null)
		{
			SetHandler("subscriptions/listen", delegate(RequestContext<SubscriptionsListenRequestParams> request, CancellationToken cancellationToken)
			{
				if (!IsJuly2026OrLaterProtocolRequest(request.JsonRpcRequest))
				{
					throw new McpProtocolException($"The method '{"subscriptions/listen"}' requires a newer protocol revision that supports per-request subscriptions; the negotiated protocol version is '{NegotiatedProtocolVersion ?? "(none)"}'.", McpErrorCode.MethodNotFound);
				}
				SubscriptionsListenRequestParams subscriptionsListenRequestParams;
				if (request.Params == null)
				{
					SubscriptionsListenRequestParams obj = new SubscriptionsListenRequestParams
					{
						Notifications = new SubscriptionsListenNotifications()
					};
					subscriptionsListenRequestParams = obj;
					request.Params = obj;
				}
				subscriptionsListenRequestParams = request.Params;
				if (subscriptionsListenRequestParams.Notifications == null)
				{
					SubscriptionsListenNotifications subscriptionsListenNotifications = (subscriptionsListenRequestParams.Notifications = new SubscriptionsListenNotifications());
				}
				return subscriptionsListenHandler(request, cancellationToken);
			}, McpJsonUtilities.JsonContext.Default.SubscriptionsListenRequestParams, McpJsonUtilities.JsonContext.Default.EmptyResult);
			return;
		}
		_requestHandlers.Set("subscriptions/listen", async delegate(SubscriptionsListenRequestParams request, JsonRpcRequest jsonRpcRequest, CancellationToken cancellationToken)
		{
			if (!IsJuly2026OrLaterProtocolRequest(jsonRpcRequest))
			{
				throw new McpProtocolException($"The method '{"subscriptions/listen"}' requires a newer protocol revision that supports per-request subscriptions; the negotiated protocol version is '{NegotiatedProtocolVersion ?? "(none)"}'.", McpErrorCode.MethodNotFound);
			}
			SubscriptionsListenNotifications subscriptionsListenNotifications = request?.Notifications ?? new SubscriptionsListenNotifications();
			if (!HasStatefulTransport())
			{
				ActiveSubscription subscription = new ActiveSubscription(jsonRpcRequest.Id, new SubscriptionsListenNotifications(), jsonRpcRequest.Context?.RelatedTransport);
				await SendSubscriptionAckAsync(subscription, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return EmptyResult.Instance;
			}
			SubscriptionsListenNotifications subscriptionsListenNotifications2 = new SubscriptionsListenNotifications();
			bool? toolsListChanged;
			if (subscriptionsListenNotifications.ToolsListChanged == true)
			{
				ServerCapabilities serverCapabilities = ServerCapabilities;
				if (serverCapabilities != null && serverCapabilities.Tools?.ListChanged == true)
				{
					toolsListChanged = true;
					goto IL_01e0;
				}
			}
			toolsListChanged = null;
			goto IL_01e0;
			IL_02fd:
			object resourceSubscriptions;
			subscriptionsListenNotifications2.ResourceSubscriptions = (IList<string>?)resourceSubscriptions;
			SubscriptionsListenNotifications granted = subscriptionsListenNotifications2;
			ActiveSubscription activeSubscription = new ActiveSubscription(jsonRpcRequest.Id, granted, jsonRpcRequest.Context?.RelatedTransport);
			_activeSubscriptions[jsonRpcRequest.Id] = activeSubscription;
			try
			{
				await SendSubscriptionAckAsync(activeSubscription, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				using (cancellationToken.Register(delegate(object? state)
				{
					((TaskCompletionSource<bool>)state).TrySetResult(result: true);
				}, taskCompletionSource))
				{
					await taskCompletionSource.Task.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			finally
			{
				_activeSubscriptions.TryRemove(jsonRpcRequest.Id, out ActiveSubscription _);
			}
			return EmptyResult.Instance;
			IL_02a0:
			bool? resourcesListChanged;
			subscriptionsListenNotifications2.ResourcesListChanged = (bool?)resourcesListChanged;
			IList<string> resourceSubscriptions2 = subscriptionsListenNotifications.ResourceSubscriptions;
			if (resourceSubscriptions2 != null && resourceSubscriptions2.Count > 0)
			{
				ServerCapabilities serverCapabilities2 = ServerCapabilities;
				if (serverCapabilities2 != null && serverCapabilities2.Resources?.Subscribe == true)
				{
					resourceSubscriptions = new List<string>(resourceSubscriptions2);
					goto IL_02fd;
				}
			}
			resourceSubscriptions = null;
			goto IL_02fd;
			IL_01e0:
			subscriptionsListenNotifications2.ToolsListChanged = toolsListChanged;
			bool? promptsListChanged;
			if (subscriptionsListenNotifications.PromptsListChanged == true)
			{
				ServerCapabilities serverCapabilities3 = ServerCapabilities;
				if (serverCapabilities3 != null && serverCapabilities3.Prompts?.ListChanged == true)
				{
					promptsListChanged = true;
					goto IL_0240;
				}
			}
			promptsListChanged = null;
			goto IL_0240;
			IL_0240:
			subscriptionsListenNotifications2.PromptsListChanged = promptsListChanged;
			if (subscriptionsListenNotifications.ResourcesListChanged == true)
			{
				ServerCapabilities serverCapabilities4 = ServerCapabilities;
				if (serverCapabilities4 != null && serverCapabilities4.Resources?.ListChanged == true)
				{
					resourcesListChanged = true;
					goto IL_02a0;
				}
			}
			resourcesListChanged = null;
			goto IL_02a0;
		}, McpJsonUtilities.JsonContext.Default.SubscriptionsListenRequestParams, McpJsonUtilities.JsonContext.Default.EmptyResult);
	}

	/// <summary>
	/// Delivers a <c>*/list_changed</c> notification triggered by a server-side collection change.
	/// </summary>
	/// <remarks>
	/// Pre-SEP-2575 clients do not open <c>subscriptions/listen</c> streams, so they keep receiving a single
	/// session-wide broadcast. Clients on the 2026-07-28 or later revision instead receive only the change notifications they explicitly
	/// requested, each routed back over the originating subscription stream and tagged with its id; the server
	/// <b>MUST NOT</b> send such a client notification types it never subscribed to.
	/// </remarks>
	private async Task SendListChangedNotificationAsync(string notificationMethod)
	{
		if (!IsJuly2026OrLaterProtocol())
		{
			await SendNotificationAsync(notificationMethod).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		foreach (ActiveSubscription subscription in _activeSubscriptions.Values)
		{
			if (GrantsListChanged(subscription.Granted, notificationMethod))
			{
				try
				{
					await SendSubscriptionNotificationAsync(subscription, notificationMethod, null, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception exception)
				{
					SubscriptionNotificationFailed(notificationMethod, subscription.Id.ToString(), exception);
				}
			}
		}
	}

	/// <summary>
	/// Sends <paramref name="method" /> over <paramref name="subscription" />'s stream, tagging it with the
	/// SEP-2575 <c>_meta</c> subscription id so clients sharing a channel (notably stdio) can demultiplex it.
	/// </summary>
	private Task SendSubscriptionNotificationAsync(ActiveSubscription subscription, string method, JsonNode? paramsNode, CancellationToken cancellationToken)
	{
		JsonObject jsonObject = (paramsNode as JsonObject) ?? new JsonObject();
		JsonObject jsonObject2 = jsonObject["_meta"] as JsonObject;
		if (jsonObject2 == null)
		{
			jsonObject2 = (JsonObject)(jsonObject["_meta"] = new JsonObject());
		}
		JsonObject jsonObject3 = jsonObject2;
		object id = subscription.Id.Id;
		JsonValue value = ((id is string value2) ? JsonValue.Create(value2) : ((!(id is long value3)) ? null : JsonValue.Create(value3)));
		jsonObject3["io.modelcontextprotocol/subscriptionId"] = value;
		JsonRpcNotification message = new JsonRpcNotification
		{
			Method = method,
			Params = jsonObject,
			Context = new JsonRpcMessageContext
			{
				RelatedTransport = subscription.RelatedTransport
			}
		};
		return SendMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Sends the SEP-2575 <c>subscriptions/acknowledged</c> notification for a subscription, carrying the
	/// notification types the server agreed to deliver. Routed back over the subscription's own stream and
	/// tagged with its id like every other subscription notification.
	/// </summary>
	private Task SendSubscriptionAckAsync(ActiveSubscription subscription, CancellationToken cancellationToken)
	{
		JsonNode paramsNode = JsonSerializer.SerializeToNode(new SubscriptionsAcknowledgedNotificationParams
		{
			Notifications = subscription.Granted
		}, McpJsonUtilities.JsonContext.Default.SubscriptionsAcknowledgedNotificationParams);
		return SendSubscriptionNotificationAsync(subscription, "notifications/subscriptions/acknowledged", paramsNode, cancellationToken);
	}

	/// <summary>Maps a <c>*/list_changed</c> method to the subscription filter flag that enables it.</summary>
	private static bool GrantsListChanged(SubscriptionsListenNotifications granted, string method)
	{
		return method switch
		{
			"notifications/tools/list_changed" => granted.ToolsListChanged == true, 
			"notifications/prompts/list_changed" => granted.PromptsListChanged == true, 
			"notifications/resources/list_changed" => granted.ResourcesListChanged == true, 
			_ => false, 
		};
	}

	private void ConfigureCompletion(McpServerOptions options)
	{
		McpRequestHandler<CompleteRequestParams, CompleteResult> mcpRequestHandler = options.Handlers.CompleteHandler;
		CompletionsCapability completionsCapability = options.Capabilities?.Completions;
		Dictionary<string, Dictionary<string, string[]>> promptCompletions = BuildAllowedValueCompletions(options.PromptCollection);
		Dictionary<string, Dictionary<string, string[]>> resourceCompletions = BuildAllowedValueCompletions(options.ResourceCollection);
		bool flag = promptCompletions != null || resourceCompletions != null;
		if (mcpRequestHandler == null && completionsCapability == null && !flag)
		{
			return;
		}
		if (mcpRequestHandler == null)
		{
			mcpRequestHandler = async (RequestContext<CompleteRequestParams> _, CancellationToken __) => new CompleteResult();
		}
		if (flag)
		{
			McpRequestHandler<CompleteRequestParams, CompleteResult> originalCompleteHandler = mcpRequestHandler;
			mcpRequestHandler = async delegate(RequestContext<CompleteRequestParams> request, CancellationToken cancellationToken)
			{
				CompleteResult completeResult = await originalCompleteHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				string[] value = null;
				Reference reference = request.Params?.Ref;
				Dictionary<string, string[]> value3;
				if (!(reference is PromptReference promptReference))
				{
					if (reference is ResourceTemplateReference resourceTemplateReference && resourceCompletions != null && resourceTemplateReference.Uri != null && resourceCompletions.TryGetValue(resourceTemplateReference.Uri, out Dictionary<string, string[]> value2))
					{
						value2.TryGetValue(request.Params.Argument.Name, out value);
					}
				}
				else if (promptCompletions != null && promptCompletions.TryGetValue(promptReference.Name, out value3))
				{
					value3.TryGetValue(request.Params.Argument.Name, out value);
				}
				if (value != null)
				{
					string value4 = request.Params.Argument.Value;
					string[] array = value;
					foreach (string text in array)
					{
						if (text.StartsWith(value4, StringComparison.OrdinalIgnoreCase))
						{
							completeResult.Completion.Values.Add(text);
						}
					}
					completeResult.Completion.Total = completeResult.Completion.Values.Count;
				}
				return completeResult;
			};
		}
		mcpRequestHandler = BuildFilterPipeline(mcpRequestHandler, options.Filters.Request.CompleteFilters);
		ServerCapabilities.Completions = new CompletionsCapability();
		SetHandler("completion/complete", mcpRequestHandler, McpJsonUtilities.JsonContext.Default.CompleteRequestParams, McpJsonUtilities.JsonContext.Default.CompleteResult);
	}

	/// <summary>
	/// Builds a lookup of primitive name/URI → (parameter name → allowed values) from the enum values
	/// in the JSON schemas of AIFunction-based prompts or resources.
	/// </summary>
	private static Dictionary<string, Dictionary<string, string[]>>? BuildAllowedValueCompletions<T>(McpServerPrimitiveCollection<T>? primitives) where T : class, IMcpServerPrimitive
	{
		if (primitives == null)
		{
			return null;
		}
		Dictionary<string, Dictionary<string, string[]>> dictionary = null;
		foreach (T primitive in primitives)
		{
			JsonElement jsonSchema;
			string key;
			if (primitive is AIFunctionMcpServerPrompt aIFunctionMcpServerPrompt)
			{
				jsonSchema = ((AIFunctionDeclaration)aIFunctionMcpServerPrompt.AIFunction).JsonSchema;
				key = aIFunctionMcpServerPrompt.ProtocolPrompt.Name;
			}
			else
			{
				if (!(primitive is AIFunctionMcpServerResource { IsTemplated: not false } aIFunctionMcpServerResource))
				{
					continue;
				}
				jsonSchema = ((AIFunctionDeclaration)aIFunctionMcpServerResource.AIFunction).JsonSchema;
				key = aIFunctionMcpServerResource.ProtocolResourceTemplate.UriTemplate;
			}
			if (!jsonSchema.TryGetProperty("properties", out var value) || value.ValueKind != JsonValueKind.Object)
			{
				continue;
			}
			Dictionary<string, string[]> dictionary2 = null;
			foreach (JsonProperty item in value.EnumerateObject())
			{
				if (!item.Value.TryGetProperty("enum", out var value2) || value2.ValueKind != JsonValueKind.Array)
				{
					continue;
				}
				List<string> list = null;
				foreach (JsonElement item2 in value2.EnumerateArray())
				{
					if (item2.ValueKind != JsonValueKind.String)
					{
						continue;
					}
					string text = item2.GetString();
					if (text != null)
					{
						if (list == null)
						{
							list = new List<string>();
						}
						list.Add(text);
					}
				}
				if (list != null)
				{
					if (dictionary2 == null)
					{
						dictionary2 = new Dictionary<string, string[]>(StringComparer.Ordinal);
					}
					dictionary2[item.Name] = list.ToArray();
				}
			}
			if (dictionary2 != null)
			{
				if (dictionary == null)
				{
					dictionary = new Dictionary<string, Dictionary<string, string[]>>(StringComparer.Ordinal);
				}
				dictionary[key] = dictionary2;
			}
		}
		return dictionary;
	}

	private void ConfigureExperimentalAndExtensions(McpServerOptions options)
	{
		ServerCapabilities.Experimental = options.Capabilities?.Experimental;
		ServerCapabilities.Extensions = options.Capabilities?.Extensions;
	}

	private void ConfigureCustomRequestHandlers(McpServerOptions options)
	{
		IList<McpServerRequestHandler> requestHandlers = options.RequestHandlers;
		if (requestHandlers == null || requestHandlers.Count <= 0)
		{
			return;
		}
		foreach (McpServerRequestHandler item in requestHandlers)
		{
			if (string.IsNullOrEmpty(item.Method))
			{
				throw new InvalidOperationException("A custom request handler registered through McpServerOptions.RequestHandlers has a null or empty Method.");
			}
			if (item.RoutingNameParameter != null && string.IsNullOrWhiteSpace(item.RoutingNameParameter))
			{
				throw new InvalidOperationException("A custom request handler registered through McpServerOptions.RequestHandlers has an empty RoutingNameParameter.");
			}
			if (_requestHandlers.ContainsKey(item.Method))
			{
				throw new InvalidOperationException($"A custom request handler registered through {"McpServerOptions"}.{"RequestHandlers"} uses the method '{item.Method}', which is already handled by the server. Custom handlers cannot replace built-in methods or other custom handlers.");
			}
			SetRawHandler(item.Method, item.Handler);
		}
	}

	private void SetRawHandler(string method, Func<JsonRpcRequest, CancellationToken, ValueTask<JsonNode?>> handler)
	{
		_requestHandlers[method] = (JsonRpcRequest request, CancellationToken ct) => handler(request, ct).AsTask();
	}

	private void ConfigureResources(McpServerOptions options)
	{
		McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> mcpRequestHandler = options.Handlers.ListResourcesHandler;
		McpRequestHandler<ListResourceTemplatesRequestParams, ListResourceTemplatesResult> mcpRequestHandler2 = options.Handlers.ListResourceTemplatesHandler;
		McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> mcpRequestHandler3 = options.Handlers.ReadResourceHandler;
		McpRequestHandler<SubscribeRequestParams, EmptyResult> mcpRequestHandler4 = options.Handlers.SubscribeToResourcesHandler;
		McpRequestHandler<UnsubscribeRequestParams, EmptyResult> mcpRequestHandler5 = options.Handlers.UnsubscribeFromResourcesHandler;
		McpServerResourceCollection resources = options.ResourceCollection;
		ResourcesCapability resourcesCapability = options.Capabilities?.Resources;
		if (mcpRequestHandler == null && mcpRequestHandler2 == null && mcpRequestHandler3 == null && mcpRequestHandler4 == null && mcpRequestHandler5 == null && resources == null && resourcesCapability == null)
		{
			return;
		}
		ServerCapabilities.Resources = new ResourcesCapability();
		if (mcpRequestHandler == null)
		{
			mcpRequestHandler = async (RequestContext<ListResourcesRequestParams> _, CancellationToken __) => new ListResourcesResult();
		}
		if (mcpRequestHandler2 == null)
		{
			mcpRequestHandler2 = async (RequestContext<ListResourceTemplatesRequestParams> _, CancellationToken __) => new ListResourceTemplatesResult();
		}
		if (mcpRequestHandler3 == null)
		{
			mcpRequestHandler3 = async delegate(RequestContext<ReadResourceRequestParams> request, CancellationToken _)
			{
				McpErrorCode errorCode = (McpProtocolVersions.UseInvalidParamsForMissingResource(request.Server.NegotiatedProtocolVersion) ? McpErrorCode.InvalidParams : McpErrorCode.ResourceNotFound);
				throw new McpProtocolException("Unknown resource URI: '" + request.Params?.Uri + "'", errorCode);
			};
		}
		if (mcpRequestHandler4 == null)
		{
			mcpRequestHandler4 = async (RequestContext<SubscribeRequestParams> _, CancellationToken __) => new EmptyResult();
		}
		if (mcpRequestHandler5 == null)
		{
			mcpRequestHandler5 = async (RequestContext<UnsubscribeRequestParams> _, CancellationToken __) => new EmptyResult();
		}
		bool? listChanged = resourcesCapability?.ListChanged;
		bool? subscribe = resourcesCapability?.Subscribe;
		if (resources != null)
		{
			McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> originalListResourcesHandler = mcpRequestHandler;
			mcpRequestHandler = async delegate(RequestContext<ListResourcesRequestParams> request, CancellationToken cancellationToken)
			{
				ListResourcesResult listResourcesResult = ((originalListResourcesHandler == null) ? new ListResourcesResult() : (await originalListResourcesHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
				ListResourcesResult listResourcesResult2 = listResourcesResult;
				if (request.Params?.Cursor == null)
				{
					foreach (McpServerResource item in resources)
					{
						Resource protocolResource = item.ProtocolResource;
						if (protocolResource != null)
						{
							listResourcesResult2.Resources.Add(protocolResource);
						}
					}
				}
				return listResourcesResult2;
			};
			McpRequestHandler<ListResourceTemplatesRequestParams, ListResourceTemplatesResult> originalListResourceTemplatesHandler = mcpRequestHandler2;
			mcpRequestHandler2 = async delegate(RequestContext<ListResourceTemplatesRequestParams> request, CancellationToken cancellationToken)
			{
				ListResourceTemplatesResult listResourceTemplatesResult = ((originalListResourceTemplatesHandler == null) ? new ListResourceTemplatesResult() : (await originalListResourceTemplatesHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
				ListResourceTemplatesResult listResourceTemplatesResult2 = listResourceTemplatesResult;
				if (request.Params?.Cursor == null)
				{
					foreach (McpServerResource item2 in resources)
					{
						if (item2.IsTemplated)
						{
							listResourceTemplatesResult2.ResourceTemplates.Add(item2.ProtocolResourceTemplate);
						}
					}
				}
				return listResourceTemplatesResult2;
			};
			McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> originalReadResourceHandler = mcpRequestHandler3;
			mcpRequestHandler3 = async (RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken) => (request.MatchedPrimitive is McpServerResource mcpServerResource) ? (await mcpServerResource.ReadAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) : (await originalReadResourceHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
			listChanged = true;
		}
		mcpRequestHandler = BuildFilterPipeline(mcpRequestHandler, options.Filters.Request.ListResourcesFilters);
		mcpRequestHandler2 = BuildFilterPipeline(mcpRequestHandler2, options.Filters.Request.ListResourceTemplatesFilters);
		mcpRequestHandler3 = BuildFilterPipeline(mcpRequestHandler3, options.Filters.Request.ReadResourceFilters, (McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> handler) => async delegate(RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken)
		{
			string text = request.Params?.Uri;
			if (text != null && resources != null)
			{
				if (resources.TryGetPrimitive(text, out McpServerResource primitive) && !primitive.IsTemplated)
				{
					request.MatchedPrimitive = primitive;
				}
				else
				{
					foreach (McpServerResource item3 in resources)
					{
						if (item3.IsMatch(text))
						{
							request.MatchedPrimitive = item3;
							break;
						}
					}
				}
			}
			try
			{
				ReadResourceResult result = await handler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				ReadResourceCompleted(request.Params?.Uri ?? string.Empty);
				return result;
			}
			catch (Exception exception)
			{
				ReadResourceError(request.Params?.Uri ?? string.Empty, exception);
				throw;
			}
		});
		mcpRequestHandler4 = BuildFilterPipeline(mcpRequestHandler4, options.Filters.Request.SubscribeToResourcesFilters);
		mcpRequestHandler5 = BuildFilterPipeline(mcpRequestHandler5, options.Filters.Request.UnsubscribeFromResourcesFilters);
		ServerCapabilities.Resources.ListChanged = listChanged;
		ServerCapabilities.Resources.Subscribe = subscribe;
		SetHandler("resources/list", mcpRequestHandler, McpJsonUtilities.JsonContext.Default.ListResourcesRequestParams, McpJsonUtilities.JsonContext.Default.ListResourcesResult);
		SetHandler("resources/templates/list", mcpRequestHandler2, McpJsonUtilities.JsonContext.Default.ListResourceTemplatesRequestParams, McpJsonUtilities.JsonContext.Default.ListResourceTemplatesResult);
		SetHandler("resources/read", mcpRequestHandler3, McpJsonUtilities.JsonContext.Default.ReadResourceRequestParams, McpJsonUtilities.JsonContext.Default.ReadResourceResult);
		SetHandler("resources/subscribe", mcpRequestHandler4, McpJsonUtilities.JsonContext.Default.SubscribeRequestParams, McpJsonUtilities.JsonContext.Default.EmptyResult);
		SetHandler("resources/unsubscribe", mcpRequestHandler5, McpJsonUtilities.JsonContext.Default.UnsubscribeRequestParams, McpJsonUtilities.JsonContext.Default.EmptyResult);
	}

	private void ConfigurePrompts(McpServerOptions options)
	{
		McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> mcpRequestHandler = options.Handlers.ListPromptsHandler;
		McpRequestHandler<GetPromptRequestParams, GetPromptResult> mcpRequestHandler2 = options.Handlers.GetPromptHandler;
		McpServerPrimitiveCollection<McpServerPrompt> prompts = options.PromptCollection;
		PromptsCapability promptsCapability = options.Capabilities?.Prompts;
		if (mcpRequestHandler == null && mcpRequestHandler2 == null && prompts == null && promptsCapability == null)
		{
			return;
		}
		ServerCapabilities.Prompts = new PromptsCapability();
		if (mcpRequestHandler == null)
		{
			mcpRequestHandler = async (RequestContext<ListPromptsRequestParams> _, CancellationToken __) => new ListPromptsResult();
		}
		if (mcpRequestHandler2 == null)
		{
			mcpRequestHandler2 = async delegate(RequestContext<GetPromptRequestParams> request, CancellationToken _)
			{
				throw new McpProtocolException("Unknown prompt: '" + request.Params?.Name + "'", McpErrorCode.InvalidParams);
			};
		}
		bool? listChanged = promptsCapability?.ListChanged;
		if (prompts != null)
		{
			McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> originalListPromptsHandler = mcpRequestHandler;
			mcpRequestHandler = async delegate(RequestContext<ListPromptsRequestParams> request, CancellationToken cancellationToken)
			{
				ListPromptsResult listPromptsResult = ((originalListPromptsHandler == null) ? new ListPromptsResult() : (await originalListPromptsHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
				ListPromptsResult listPromptsResult2 = listPromptsResult;
				if (request.Params?.Cursor == null)
				{
					foreach (McpServerPrompt item in prompts)
					{
						listPromptsResult2.Prompts.Add(item.ProtocolPrompt);
					}
				}
				return listPromptsResult2;
			};
			McpRequestHandler<GetPromptRequestParams, GetPromptResult> originalGetPromptHandler = mcpRequestHandler2;
			mcpRequestHandler2 = (RequestContext<GetPromptRequestParams> request, CancellationToken cancellationToken) => (ValueTask<GetPromptResult>)((request.MatchedPrimitive is McpServerPrompt mcpServerPrompt) ? ((ValueType)mcpServerPrompt.GetAsync(request, cancellationToken)) : ((ValueType)originalGetPromptHandler(request, cancellationToken)));
			listChanged = true;
		}
		mcpRequestHandler = BuildFilterPipeline(mcpRequestHandler, options.Filters.Request.ListPromptsFilters);
		mcpRequestHandler2 = BuildFilterPipeline(mcpRequestHandler2, options.Filters.Request.GetPromptFilters, (McpRequestHandler<GetPromptRequestParams, GetPromptResult> handler) => async delegate(RequestContext<GetPromptRequestParams> request, CancellationToken cancellationToken)
		{
			string text = request.Params?.Name;
			if (text != null && prompts != null && prompts.TryGetPrimitive(text, out McpServerPrompt primitive))
			{
				request.MatchedPrimitive = primitive;
			}
			try
			{
				GetPromptResult result = await handler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				GetPromptCompleted(request.Params?.Name ?? string.Empty);
				return result;
			}
			catch (Exception exception)
			{
				GetPromptError(request.Params?.Name ?? string.Empty, exception);
				throw;
			}
		});
		ServerCapabilities.Prompts.ListChanged = listChanged;
		SetHandler("prompts/list", mcpRequestHandler, McpJsonUtilities.JsonContext.Default.ListPromptsRequestParams, McpJsonUtilities.JsonContext.Default.ListPromptsResult);
		SetHandler("prompts/get", mcpRequestHandler2, McpJsonUtilities.JsonContext.Default.GetPromptRequestParams, McpJsonUtilities.JsonContext.Default.GetPromptResult);
	}

	private void ConfigureTools(McpServerOptions options)
	{
		McpRequestHandler<ListToolsRequestParams, ListToolsResult> mcpRequestHandler = options.Handlers.ListToolsHandler;
		McpRequestHandler<CallToolRequestParams, CallToolResult> mcpRequestHandler2 = options.Handlers.CallToolHandler;
		McpRequestHandler<CallToolRequestParams, ResultOrAlternate<CallToolResult>> mcpRequestHandler3 = options.Handlers.CallToolWithAlternateHandler;
		McpServerPrimitiveCollection<McpServerTool> tools = options.ToolCollection;
		ToolsCapability toolsCapability = options.Capabilities?.Tools;
		if (mcpRequestHandler == null && mcpRequestHandler2 == null && mcpRequestHandler3 == null && tools == null && toolsCapability == null)
		{
			return;
		}
		ServerCapabilities.Tools = new ToolsCapability();
		if (mcpRequestHandler == null)
		{
			mcpRequestHandler = async (RequestContext<ListToolsRequestParams> _, CancellationToken __) => new ListToolsResult();
		}
		bool? listChanged = toolsCapability?.ListChanged;
		IList<McpRequestFilter<CallToolRequestParams, CallToolResult>> callToolFilters = options.Filters.Request.CallToolFilters;
		IList<McpRequestInvocationFilter<CallToolRequestParams, ResultOrAlternate<CallToolResult>>> callToolWithAlternateFilters = options.Filters.Request.CallToolWithAlternateFilters;
		if (mcpRequestHandler3 != null && callToolFilters.Count > 0)
		{
			throw new InvalidOperationException("Cannot apply CallToolFilters when an explicit CallToolWithAlternateHandler is configured. The alternate handler replaces the ordinary tool-call pipeline. Move the behavior to CallToolWithAlternateFilters or remove the explicit alternate handler.");
		}
		if (tools != null)
		{
			McpRequestHandler<ListToolsRequestParams, ListToolsResult> originalListToolsHandler = mcpRequestHandler;
			mcpRequestHandler = async delegate(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
			{
				ListToolsResult listToolsResult = ((originalListToolsHandler == null) ? new ListToolsResult() : (await originalListToolsHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
				ListToolsResult listToolsResult2 = listToolsResult;
				if (request.Params?.Cursor == null)
				{
					bool flag = McpSessionHandler.SupportsNaturalOutputSchemas(request.Server.NegotiatedProtocolVersion);
					foreach (McpServerTool item2 in tools)
					{
						Tool item = ((flag || !(item2 is AIFunctionMcpServerTool aIFunctionMcpServerTool)) ? item2.ProtocolTool : aIFunctionMcpServerTool.BuildLegacyWireProtocolTool());
						listToolsResult2.Tools.Add(item);
					}
				}
				return listToolsResult2;
			};
			listChanged = true;
		}
		mcpRequestHandler = BuildFilterPipeline(mcpRequestHandler, options.Filters.Request.ListToolsFilters);
		if (mcpRequestHandler3 != null)
		{
			if (tools != null)
			{
				McpRequestHandler<CallToolRequestParams, ResultOrAlternate<CallToolResult>> originalHandler = mcpRequestHandler3;
				mcpRequestHandler3 = delegate(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
				{
					MatchTool(request, tools);
					return (ValueTask<ResultOrAlternate<CallToolResult>>)((request.MatchedPrimitive is McpServerTool tool) ? ((ValueType)InvokeToolWithAlternate(tool, request, cancellationToken)) : ((ValueType)originalHandler(request, cancellationToken)));
				};
			}
			mcpRequestHandler3 = BuildInvocationFilterPipeline<CallToolRequestParams, ResultOrAlternate<CallToolResult>>(mcpRequestHandler3, callToolWithAlternateFilters, BuildInitialAlternateToolFilter(tools));
		}
		else
		{
			if (mcpRequestHandler2 == null)
			{
				mcpRequestHandler2 = async delegate(RequestContext<CallToolRequestParams> request, CancellationToken _)
				{
					throw new McpProtocolException("Unknown tool: '" + request.Params?.Name + "'", McpErrorCode.InvalidParams);
				};
			}
			if (tools != null)
			{
				McpRequestHandler<CallToolRequestParams, CallToolResult> originalHandler2 = mcpRequestHandler2;
				mcpRequestHandler2 = (RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken) => (ValueTask<CallToolResult>)((request.MatchedPrimitive is McpServerTool mcpServerTool) ? ((ValueType)mcpServerTool.InvokeAsync(request, cancellationToken)) : ((ValueType)originalHandler2(request, cancellationToken)));
			}
			mcpRequestHandler2 = BuildFilterPipeline(mcpRequestHandler2, callToolFilters);
			mcpRequestHandler3 = BuildComposedCallToolHandler(mcpRequestHandler2, callToolWithAlternateFilters, tools);
		}
		ServerCapabilities.Tools.ListChanged = listChanged;
		SetHandler("tools/list", mcpRequestHandler, McpJsonUtilities.JsonContext.Default.ListToolsRequestParams, McpJsonUtilities.JsonContext.Default.ListToolsResult);
		SetWithAlternateHandler("tools/call", mcpRequestHandler3, McpJsonUtilities.JsonContext.Default.CallToolRequestParams, McpJsonUtilities.JsonContext.Default.CallToolResult);
	}

	private static async ValueTask<ResultOrAlternate<CallToolResult>> InvokeToolWithAlternate(McpServerTool tool, RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
	{
		return await tool.InvokeAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private McpRequestHandler<CallToolRequestParams, ResultOrAlternate<CallToolResult>> BuildComposedCallToolHandler(McpRequestHandler<CallToolRequestParams, CallToolResult> callToolHandler, IList<McpRequestInvocationFilter<CallToolRequestParams, ResultOrAlternate<CallToolResult>>> callToolWithAlternateFilters, McpServerPrimitiveCollection<McpServerTool>? tools)
	{
		return async delegate(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
		{
			MatchTool(request, tools);
			ComposedCallToolInvocationState invocation = new ComposedCallToolInvocationState();
			McpRequestHandler<CallToolRequestParams, ResultOrAlternate<CallToolResult>> mcpRequestHandler = BuildInvocationFilterPipeline(InvokeOrdinaryPipelineAsync, callToolWithAlternateFilters);
			try
			{
				ResultOrAlternate<CallToolResult> result = await mcpRequestHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				LogToolCallLifecycles(request, invocation.CompleteOuter(result));
				return result;
			}
			catch (Exception ex)
			{
				LogToolCallLifecycles(request, invocation.CompleteOuterException(ex, cancellationToken.IsCancellationRequested));
				if ((ex is OperationCanceledException && cancellationToken.IsCancellationRequested) || ex is McpProtocolException || ex is InputRequiredException)
				{
					throw;
				}
				return CreateToolCallErrorResult(request, ex);
			}
			async ValueTask<ResultOrAlternate<CallToolResult>> InvokeOrdinaryPipelineAsync(RequestContext<CallToolRequestParams> ordinaryRequest, CancellationToken ordinaryCancellationToken)
			{
				try
				{
					MatchTool(ordinaryRequest, tools);
					CallToolResult callToolResult = await callToolHandler(ordinaryRequest, ordinaryCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					LogToolCallLifecycles(ordinaryRequest, invocation.RecordOrdinaryResult(callToolResult));
					return callToolResult;
				}
				catch (Exception ex2)
				{
					LogToolCallLifecycles(ordinaryRequest, invocation.RecordOrdinaryException(ex2, ordinaryCancellationToken.IsCancellationRequested));
					if ((ex2 is OperationCanceledException && ordinaryCancellationToken.IsCancellationRequested) || ex2 is McpProtocolException || ex2 is InputRequiredException)
					{
						throw;
					}
					return CreateToolCallErrorResult(ordinaryRequest, ex2);
				}
			}
		};
	}

	private McpRequestInvocationFilter<CallToolRequestParams, ResultOrAlternate<CallToolResult>> BuildInitialAlternateToolFilter(McpServerPrimitiveCollection<McpServerTool>? tools)
	{
		return async delegate(RequestContext<CallToolRequestParams> request, McpRequestHandler<CallToolRequestParams, ResultOrAlternate<CallToolResult>> handler, CancellationToken cancellationToken)
		{
			MatchTool(request, tools);
			try
			{
				ResultOrAlternate<CallToolResult> resultOrAlternate = await handler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (!resultOrAlternate.IsAlternate)
				{
					ToolCallCompleted(request.Params?.Name ?? string.Empty, resultOrAlternate.Result.IsError ?? false);
				}
				return resultOrAlternate;
			}
			catch (Exception ex)
			{
				if ((!(ex is OperationCanceledException) || !cancellationToken.IsCancellationRequested) && !(ex is InputRequiredException))
				{
					ToolCallError(request.Params?.Name ?? string.Empty, ex);
				}
				if ((ex is OperationCanceledException && cancellationToken.IsCancellationRequested) || ex is McpProtocolException || ex is InputRequiredException)
				{
					throw;
				}
				return CreateToolCallErrorResult(request, ex);
			}
		};
	}

	private static void MatchTool(RequestContext<CallToolRequestParams> request, McpServerPrimitiveCollection<McpServerTool>? tools)
	{
		string text = request.Params?.Name;
		if (text != null && tools != null && tools.TryGetPrimitive(text, out McpServerTool primitive))
		{
			request.MatchedPrimitive = primitive;
		}
	}

	private static CallToolResult CreateToolCallErrorResult(RequestContext<CallToolRequestParams> request, Exception exception)
	{
		CallToolResult obj = new CallToolResult
		{
			IsError = true
		};
		int num = 1;
		List<ContentBlock> list = new List<ContentBlock>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<ContentBlock> span = CollectionsMarshal.AsSpan(list);
		int index = 0;
		span[index] = new TextContentBlock
		{
			Text = ((exception is McpException) ? ("An error occurred invoking '" + request.Params?.Name + "': " + exception.Message) : ("An error occurred invoking '" + request.Params?.Name + "'."))
		};
		obj.Content = list;
		return obj;
	}

	private void LogToolCallLifecycles(RequestContext<CallToolRequestParams> request, IReadOnlyList<ToolCallLifecycle> lifecycles)
	{
		string toolName = request.Params?.Name ?? string.Empty;
		foreach (ToolCallLifecycle lifecycle in lifecycles)
		{
			CallToolResult result = lifecycle.Result;
			if (result != null)
			{
				ToolCallCompleted(toolName, result.IsError ?? false);
			}
			else if ((!(lifecycle.Exception is OperationCanceledException) || !lifecycle.CancellationRequested) && !(lifecycle.Exception is InputRequiredException))
			{
				ToolCallError(toolName, lifecycle.Exception);
			}
		}
	}

	private void ConfigureLogging(McpServerOptions options)
	{
		McpRequestHandler<SetLevelRequestParams, EmptyResult> setLoggingLevelHandler = options.Handlers.SetLoggingLevelHandler;
		if (setLoggingLevelHandler != null)
		{
			setLoggingLevelHandler = BuildFilterPipeline(setLoggingLevelHandler, options.Filters.Request.SetLoggingLevelFilters);
		}
		ServerCapabilities.Logging = new LoggingCapability();
		_requestHandlers.Set("logging/setLevel", delegate(SetLevelRequestParams request, JsonRpcRequest jsonRpcRequest, CancellationToken cancellationToken)
		{
			if (IsJuly2026OrLaterProtocolRequest(jsonRpcRequest))
			{
				throw new McpProtocolException($"The method '{"logging/setLevel"}' is not available on protocol version '{jsonRpcRequest.Context?.ProtocolVersion ?? NegotiatedProtocolVersion}'. Use per-request _meta/{"io.modelcontextprotocol/logLevel"} instead.", McpErrorCode.MethodNotFound);
			}
			if (request != null)
			{
				if (_loggingLevel == null)
				{
					Interlocked.CompareExchange(ref _loggingLevel, new StrongBox<LoggingLevel>(request.Level), null);
				}
				_loggingLevel.Value = request.Level;
			}
			return (setLoggingLevelHandler != null) ? InvokeHandlerAsync(setLoggingLevelHandler, request, jsonRpcRequest, cancellationToken) : new ValueTask<EmptyResult>(new EmptyResult());
		}, McpJsonUtilities.JsonContext.Default.SetLevelRequestParams, McpJsonUtilities.JsonContext.Default.EmptyResult);
	}

	private ValueTask<TResult> InvokeHandlerAsync<TParams, TResult>(McpRequestHandler<TParams, TResult> handler, TParams args, JsonRpcRequest jsonRpcRequest, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!_servicesScopePerRequest)
		{
			return handler(new RequestContext<TParams>(CreateDestinationBoundServer(jsonRpcRequest), jsonRpcRequest, args), cancellationToken);
		}
		return InvokeScopedAsync(handler, args, jsonRpcRequest, cancellationToken);
		async ValueTask<TResult> InvokeScopedAsync(McpRequestHandler<TParams, TResult> mcpRequestHandler, TParams parameters, JsonRpcRequest jsonRpcRequest2, CancellationToken cancellationToken2)
		{
			AsyncServiceScope? scope = Services?.GetService<IServiceScopeFactory>()?.CreateAsyncScope();
			TResult result;
			try
			{
				result = await mcpRequestHandler(new RequestContext<TParams>(CreateDestinationBoundServer(jsonRpcRequest2), jsonRpcRequest2, parameters)
				{
					Services = (scope?.ServiceProvider ?? Services)
				}, cancellationToken2).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				if (scope.HasValue)
				{
					await scope.Value.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			return result;
		}
	}

	private DestinationBoundMcpServer CreateDestinationBoundServer(JsonRpcRequest jsonRpcRequest)
	{
		DestinationBoundMcpServer destinationBoundMcpServer = new DestinationBoundMcpServer(this, jsonRpcRequest.Context?.RelatedTransport, jsonRpcRequest.Context);
		if (_mrtrContextsByRequestId.TryRemove(jsonRpcRequest.Id, out MrtrContext value))
		{
			destinationBoundMcpServer.ActiveMrtrContext = value;
		}
		return destinationBoundMcpServer;
	}

	private void SetHandler<TParams, TResult>(string method, McpRequestHandler<TParams, TResult> handler, JsonTypeInfo<TParams> requestTypeInfo, JsonTypeInfo<TResult> responseTypeInfo)
	{
		if (typeof(ICacheableResult).IsAssignableFrom(typeof(TResult)))
		{
			McpRequestHandler<TParams, TResult> innerHandler = handler;
			handler = async delegate(RequestContext<TParams> request, CancellationToken cancellationToken)
			{
				TResult obj = await innerHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (obj is ICacheableResult cacheableResult && IsJuly2026OrLaterProtocolRequest(request.JsonRpcRequest))
				{
					ICacheableResult cacheableResult2 = cacheableResult;
					TimeSpan? timeToLive = cacheableResult2.TimeToLive;
					timeToLive.GetValueOrDefault();
					if (!timeToLive.HasValue)
					{
						TimeSpan zero = TimeSpan.Zero;
						cacheableResult2.TimeToLive = zero;
					}
					cacheableResult2 = cacheableResult;
					CacheScope? cacheScope = cacheableResult2.CacheScope;
					cacheScope.GetValueOrDefault();
					if (!cacheScope.HasValue)
					{
						CacheScope value = CacheScope.Private;
						cacheableResult2.CacheScope = value;
					}
				}
				return obj;
			};
		}
		if (typeof(Result).IsAssignableFrom(typeof(TResult)))
		{
			McpRequestHandler<TParams, TResult> innerHandler2 = handler;
			handler = async delegate(RequestContext<TParams> request, CancellationToken cancellationToken)
			{
				TResult obj = await innerHandler2(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (obj is Result { ResultType: null } result && IsJuly2026OrLaterProtocolRequest(request.JsonRpcRequest))
				{
					result.ResultType = "complete";
				}
				return obj;
			};
		}
		_requestHandlers.Set(method, (TParams request, JsonRpcRequest jsonRpcRequest, CancellationToken cancellationToken) => InvokeHandlerAsync(handler, request, jsonRpcRequest, cancellationToken), requestTypeInfo, responseTypeInfo);
	}

	private void SetWithAlternateHandler<TParams, TResult>(string method, McpRequestHandler<TParams, ResultOrAlternate<TResult>> handler, JsonTypeInfo<TParams> requestTypeInfo, JsonTypeInfo<TResult> responseTypeInfo) where TResult : Result
	{
		McpRequestHandler<TParams, ResultOrAlternate<TResult>> innerHandler = handler;
		handler = async delegate(RequestContext<TParams> request, CancellationToken cancellationToken)
		{
			ResultOrAlternate<TResult> resultOrAlternate = await innerHandler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (!resultOrAlternate.IsAlternate)
			{
				TResult result = resultOrAlternate.Result;
				if (result != null && result.ResultType == null && IsJuly2026OrLaterProtocolRequest(request.JsonRpcRequest))
				{
					result.ResultType = "complete";
				}
			}
			return resultOrAlternate;
		};
		_requestHandlers.SetWithAlternate(method, (TParams request, JsonRpcRequest jsonRpcRequest, CancellationToken cancellationToken) => InvokeHandlerAsync(handler, request, jsonRpcRequest, cancellationToken), requestTypeInfo, responseTypeInfo);
	}

	private static McpRequestHandler<TParams, TResult> BuildFilterPipeline<TParams, TResult>(McpRequestHandler<TParams, TResult> baseHandler, IList<McpRequestFilter<TParams, TResult>> filters, McpRequestFilter<TParams, TResult>? initialHandler = null)
	{
		McpRequestHandler<TParams, TResult> mcpRequestHandler = baseHandler;
		for (int num = filters.Count - 1; num >= 0; num--)
		{
			mcpRequestHandler = filters[num](mcpRequestHandler);
		}
		if (initialHandler != null)
		{
			mcpRequestHandler = initialHandler(mcpRequestHandler);
		}
		return mcpRequestHandler;
	}

	private static McpRequestHandler<TParams, TResult> BuildInvocationFilterPipeline<TParams, TResult>(McpRequestHandler<TParams, TResult> baseHandler, IList<McpRequestInvocationFilter<TParams, TResult>> filters, McpRequestInvocationFilter<TParams, TResult>? initialHandler = null)
	{
		McpRequestHandler<TParams, TResult> mcpRequestHandler = baseHandler;
		for (int num = filters.Count - 1; num >= 0; num--)
		{
			McpRequestHandler<TParams, TResult> next = mcpRequestHandler;
			McpRequestInvocationFilter<TParams, TResult> filter = filters[num];
			mcpRequestHandler = (RequestContext<TParams> request, CancellationToken cancellationToken) => filter(request, next, cancellationToken);
		}
		if (initialHandler != null)
		{
			McpRequestHandler<TParams, TResult> next2 = mcpRequestHandler;
			mcpRequestHandler = (RequestContext<TParams> request, CancellationToken cancellationToken) => initialHandler(request, next2, cancellationToken);
		}
		return mcpRequestHandler;
	}

	private JsonRpcMessageFilter BuildMessageFilterPipeline(IList<McpMessageFilter> filters)
	{
		if (filters.Count == 0)
		{
			return (Func<JsonRpcMessage, CancellationToken, Task> next) => next;
		}
		return delegate(Func<JsonRpcMessage, CancellationToken, Task> next)
		{
			McpMessageHandler mcpMessageHandler = async delegate(MessageContext context, CancellationToken cancellationToken)
			{
				await next(context.JsonRpcMessage, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			};
			McpMessageHandler current = mcpMessageHandler;
			for (int num = filters.Count - 1; num >= 0; num--)
			{
				current = filters[num](current);
			}
			return async delegate(JsonRpcMessage message, CancellationToken cancellationToken)
			{
				if (message.Context == null)
				{
					message.Context = new JsonRpcMessageContext();
				}
				MessageContext context = new MessageContext(new DestinationBoundMcpServer(this, message.Context.RelatedTransport, message.Context), message);
				await current(context, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			};
		};
	}

	private void UpdateEndpointNameWithClientInfo()
	{
		if (ClientInfo != null)
		{
			_endpointName = $"{_serverOnlyEndpointName}, Client ({ClientInfo.Name} {ClientInfo.Version})";
		}
	}

	/// <summary>Maps a <see cref="T:Microsoft.Extensions.Logging.LogLevel" /> to a <see cref="P:ModelContextProtocol.Server.McpServerImpl.LoggingLevel" />.</summary>
	internal static LoggingLevel ToLoggingLevel(LogLevel level)
	{
		return level switch
		{
			LogLevel.Trace => ModelContextProtocol.Protocol.LoggingLevel.Debug, 
			LogLevel.Debug => ModelContextProtocol.Protocol.LoggingLevel.Debug, 
			LogLevel.Information => ModelContextProtocol.Protocol.LoggingLevel.Info, 
			LogLevel.Warning => ModelContextProtocol.Protocol.LoggingLevel.Warning, 
			LogLevel.Error => ModelContextProtocol.Protocol.LoggingLevel.Error, 
			LogLevel.Critical => ModelContextProtocol.Protocol.LoggingLevel.Critical, 
			_ => ModelContextProtocol.Protocol.LoggingLevel.Emergency, 
		};
	}

	/// <summary>
	/// Checks whether the negotiated protocol version enables MRTR per SEP-2322 (first available in the
	/// 2026-07-28 revision). MRTR rides on the 2026-07-28 revision, so this is the MRTR-meaning alias of
	/// <see cref="M:ModelContextProtocol.McpSession.IsJuly2026OrLaterProtocol" /> - use it at the input-required/handler-suspension
	/// sites where the intent is "the client understands <see cref="T:ModelContextProtocol.Protocol.InputRequiredResult" />" rather than
	/// "the peer speaks the 2026-07-28 or later revision".
	/// </summary>
	internal bool ClientSupportsMrtr()
	{
		return IsJuly2026OrLaterProtocol();
	}

	/// <summary>
	/// Returns <see langword="true" /> when the session is stateful - the same server instance handles
	/// subsequent requests on the same session. The legacy backcompat resolver in
	/// <see cref="M:ModelContextProtocol.Server.McpServerImpl.InvokeWithInputRequiredResultHandlingAsync(System.Func{ModelContextProtocol.Protocol.JsonRpcRequest,System.Threading.CancellationToken,System.Threading.Tasks.Task{System.Text.Json.Nodes.JsonNode}},ModelContextProtocol.Protocol.JsonRpcRequest,System.Threading.CancellationToken)" /> needs a stateful session so it can send
	/// <c>elicitation/create</c> / <c>sampling/createMessage</c> / <c>roots/list</c> to the client and
	/// retry the handler with the responses.
	/// </summary>
	internal bool HasStatefulTransport()
	{
		return !(_sessionTransport is StreamableHttpServerTransport streamableHttpServerTransport) || !streamableHttpServerTransport.Stateless;
	}

	/// <summary>
	/// Returns <see langword="true" /> when the given request was negotiated under the 2026-07-28 or later protocol
	/// revision, derived from the per-request <c>_meta</c>/<c>MCP-Protocol-Version</c> value (so it works
	/// for requests over stateless HTTP) and falling back to the session-negotiated version.
	/// </summary>
	private bool IsJuly2026OrLaterProtocolRequest(JsonRpcRequest? request)
	{
		return IsJuly2026OrLaterProtocolRequest(request?.Context);
	}

	/// <inheritdoc cref="M:ModelContextProtocol.Server.McpServerImpl.IsJuly2026OrLaterProtocolRequest(ModelContextProtocol.Protocol.JsonRpcRequest)" />
	internal bool IsJuly2026OrLaterProtocolRequest(JsonRpcMessageContext? requestContext)
	{
		return McpProtocolVersions.IsJuly2026OrLaterProtocolVersion(requestContext?.ProtocolVersion ?? NegotiatedProtocolVersion);
	}

	/// <summary>
	/// Invokes a handler and catches <see cref="T:ModelContextProtocol.Protocol.InputRequiredException" /> to convert it to an
	/// <see cref="T:ModelContextProtocol.Protocol.InputRequiredResult" /> JSON response. When MRTR is negotiated or the server is stateless,
	/// the result is serialized directly. Otherwise, input requests are resolved via standard JSON-RPC
	/// calls (elicitation, sampling, roots) and the handler is retried with the responses - allowing
	/// MRTR-native tools to work transparently with clients that don't support MRTR.
	/// </summary>
	private async Task<JsonNode?> InvokeWithInputRequiredResultHandlingAsync(Func<JsonRpcRequest, CancellationToken, Task<JsonNode?>> handler, JsonRpcRequest request, CancellationToken cancellationToken)
	{
		int retry = 0;
		Exception inputRequiredException;
		while (true)
		{
			inputRequiredException = null;
			InputRequiredResult inputRequiredResult;
			try
			{
				JsonNode result = await handler(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				InputRequiredResult returnedInputRequiredResult = GetReturnedInputRequiredResult(result);
				if (returnedInputRequiredResult == null)
				{
					return result;
				}
				inputRequiredResult = returnedInputRequiredResult;
			}
			catch (InputRequiredException ex)
			{
				inputRequiredResult = ex.Result;
				inputRequiredException = ex;
			}
			if (ClientSupportsMrtr())
			{
				return SerializeInputRequiredResult(inputRequiredResult);
			}
			if (!HasStatefulTransport())
			{
				throw new McpException("A tool handler returned an incomplete result, but the server is stateless and the client does not support MRTR. MRTR-native tools require either an MRTR-capable client or a stateful server for backward-compatible resolution.", inputRequiredException);
			}
			IDictionary<string, InputRequest> inputRequests = inputRequiredResult.InputRequests;
			if (inputRequests == null || inputRequests.Count <= 0)
			{
				throw new McpException("A tool handler returned an incomplete result without input requests, and the client does not support MRTR.", inputRequiredException);
			}
			if (retry >= 10)
			{
				break;
			}
			IDictionary<string, InputResponse> value = await ResolveInputRequestsAsync(CreateDestinationBoundServer(request), inputRequests, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			JsonObject jsonObject = (request.Params?.DeepClone() as JsonObject) ?? new JsonObject();
			jsonObject["inputResponses"] = JsonSerializer.SerializeToNode(value, McpJsonUtilities.JsonContext.Default.IDictionaryStringInputResponse);
			string requestState = inputRequiredResult.RequestState;
			if (requestState != null)
			{
				jsonObject["requestState"] = requestState;
			}
			else
			{
				jsonObject.Remove("requestState");
			}
			request = new JsonRpcRequest
			{
				Id = request.Id,
				Method = request.Method,
				Params = jsonObject,
				Context = request.Context
			};
			retry++;
		}
		throw new McpException($"MRTR-native tool exceeded {10} retry rounds without completing.", inputRequiredException);
	}

	/// <summary>
	/// Resolves a batch of MRTR input requests concurrently by dispatching each as a standard
	/// JSON-RPC request to the client. The requests are routed via <paramref name="destinationServer" />
	/// so they go out through the POST's response stream (matching the behavior of tool-initiated
	/// server-to-client requests like <c>server.SampleAsync</c>) and avoid racing with the client's
	/// GET stream startup. On the first failure all remaining handlers are cancelled so user-facing
	/// flows (sampling/elicitation prompts) don't keep running once the caller has given up, and
	/// exceptions from late-completing tasks are observed before the original exception is rethrown.
	/// </summary>
	private static async Task<IDictionary<string, InputResponse>> ResolveInputRequestsAsync(McpServer destinationServer, IDictionary<string, InputRequest> inputRequests, CancellationToken cancellationToken)
	{
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		(string Key, Task<InputResponse> Task)[] keyed = new(string, Task<InputResponse>)[inputRequests.Count];
		int num = 0;
		foreach (KeyValuePair<string, InputRequest> inputRequest in inputRequests)
		{
			keyed[num++] = (Key: inputRequest.Key, Task: ResolveInputRequestAsync(destinationServer, inputRequest.Value, linkedCts.Token));
		}
		try
		{
			await Task.WhenAll(Array.ConvertAll(keyed, ((string Key, Task<InputResponse> Task) k) => k.Task)).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			linkedCts.Cancel();
			try
			{
				await Task.WhenAll(Array.ConvertAll(keyed, ((string Key, Task<InputResponse> Task) k) => k.Task)).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
			}
			throw;
		}
		Dictionary<string, InputResponse> dictionary = new Dictionary<string, InputResponse>(keyed.Length);
		(string, Task<InputResponse>)[] array = keyed;
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			(string, Task<InputResponse>) tuple = array[num2];
			string item = tuple.Item1;
			Task<InputResponse> item2 = tuple.Item2;
			dictionary[item] = item2.Result;
		}
		return dictionary;
	}

	/// <summary>
	/// Resolves a single MRTR <see cref="T:ModelContextProtocol.Protocol.InputRequest" /> by dispatching it as a standard JSON-RPC
	/// request to the client via <paramref name="destinationServer" />. This is the server-side mirror
	/// of the client's input resolution logic, used for backward compatibility when the client doesn't
	/// support MRTR.
	/// </summary>
	private static async Task<InputResponse> ResolveInputRequestAsync(McpServer destinationServer, InputRequest inputRequest, CancellationToken cancellationToken)
	{
		switch (inputRequest.Method)
		{
		case "elicitation/create":
		{
			ElicitRequestParams requestParams2 = inputRequest.ElicitationParams ?? throw new McpException("Failed to deserialize elicitation parameters from MRTR input request.");
			return InputResponse.FromElicitResult(await destinationServer.ElicitAsync(requestParams2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		}
		case "sampling/createMessage":
		{
			CreateMessageRequestParams requestParams3 = inputRequest.SamplingParams ?? throw new McpException("Failed to deserialize sampling parameters from MRTR input request.");
			return InputResponse.FromSamplingResult(await destinationServer.SampleAsync(requestParams3, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		}
		case "roots/list":
		{
			ListRootsRequestParams requestParams = inputRequest.RootsParams ?? new ListRootsRequestParams();
			return InputResponse.FromRootsResult(await destinationServer.RequestRootsAsync(requestParams, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
		}
		default:
			throw new McpException("Unsupported input request method: '" + inputRequest.Method + "'.");
		}
	}

	private static JsonNode? SerializeInputRequiredResult(InputRequiredResult inputRequiredResult)
	{
		return JsonSerializer.SerializeToNode(inputRequiredResult, McpJsonUtilities.JsonContext.Default.InputRequiredResult);
	}

	/// <summary>
	/// Detects an <see cref="T:ModelContextProtocol.Protocol.InputRequiredResult" /> that a handler surfaced by RETURNING it through the alternate
	/// result path (rather than throwing <see cref="T:ModelContextProtocol.Protocol.InputRequiredException" />), so both forms can be resolved
	/// identically for clients that don't natively support MRTR. Returns <see langword="null" /> for any other result.
	/// </summary>
	private static InputRequiredResult? GetReturnedInputRequiredResult(JsonNode? result)
	{
		if (result is JsonObject jsonObject && jsonObject.TryGetPropertyValue("resultType", out JsonNode jsonNode) && jsonNode != null && jsonNode.GetValueKind() == JsonValueKind.String && jsonNode.GetValue<string>() == "input_required")
		{
			return result.Deserialize(McpJsonUtilities.JsonContext.Default.InputRequiredResult);
		}
		return null;
	}

	/// <summary>
	/// Wraps MRTR-eligible request handlers so that when a handler calls ElicitAsync/SampleAsync/RequestRootsAsync,
	/// an <see cref="T:ModelContextProtocol.Protocol.InputRequiredResult" /> is returned early and the handler is suspended until the retry arrives.
	/// </summary>
	private void ConfigureMrtr()
	{
		WrapHandlerWithMrtr("tools/call");
		WrapHandlerWithMrtr("prompts/get");
		WrapHandlerWithMrtr("resources/read");
	}

	/// <summary>
	/// Replaces an existing request handler entry with an MRTR-aware wrapper that supports
	/// handler suspension and <see cref="T:ModelContextProtocol.Protocol.InputRequiredResult" /> responses.
	/// </summary>
	private void WrapHandlerWithMrtr(string method)
	{
		if (!_requestHandlers.TryGetValue(method, out Func<JsonRpcRequest, CancellationToken, Task<JsonNode?>> originalHandler))
		{
			return;
		}
		_requestHandlers[method] = async delegate(JsonRpcRequest request, CancellationToken cancellationToken)
		{
			if (request.Params is JsonObject jsonObject && jsonObject.TryGetPropertyValue("requestState", out JsonNode jsonNode) && jsonNode != null && jsonNode.GetValueKind() == JsonValueKind.String)
			{
				string value = jsonNode.GetValue<string>();
				if (value != null && _mrtrContinuations.TryRemove(value, out MrtrContinuation value2))
				{
					IDictionary<string, InputResponse> dictionary = null;
					if (jsonObject.TryGetPropertyValue("inputResponses", out JsonNode jsonNode2) && jsonNode2 != null)
					{
						dictionary = jsonNode2.Deserialize(McpJsonUtilities.JsonContext.Default.IDictionaryStringInputResponse);
					}
					MrtrExchange pendingExchange = value2.PendingExchange;
					Task<MrtrExchange> exchangeTask = value2.MrtrContext.ResetForNextExchange(pendingExchange);
					if (dictionary != null && dictionary.TryGetValue(pendingExchange.Key, out var value3))
					{
						if (!pendingExchange.ResponseTcs.TrySetResult(value3))
						{
							throw new McpProtocolException("MRTR exchange '" + pendingExchange.Key + "' was already completed (possibly cancelled).", McpErrorCode.InternalError);
						}
					}
					else if (!pendingExchange.ResponseTcs.TrySetException(new McpProtocolException("Missing input response for key '" + pendingExchange.Key + "'.", McpErrorCode.InvalidParams)))
					{
						throw new McpProtocolException("MRTR exchange '" + pendingExchange.Key + "' was already completed (possibly cancelled).", McpErrorCode.InternalError);
					}
					return await AwaitMrtrHandlerAsync(value2.HandlerTask, value2, exchangeTask, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			if (!ClientSupportsMrtr() || !HasStatefulTransport())
			{
				return await InvokeWithInputRequiredResultHandlingAsync(originalHandler, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			MrtrContext mrtrContext = new MrtrContext();
			CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_mrtrContextsByRequestId[request.Id] = mrtrContext;
			Task<JsonNode> handlerTask;
			try
			{
				handlerTask = originalHandler(request, cancellationTokenSource.Token);
			}
			finally
			{
				_mrtrContextsByRequestId.TryRemove(request.Id, out MrtrContext _);
			}
			MrtrContinuation continuation = new MrtrContinuation(cancellationTokenSource, handlerTask, mrtrContext);
			Interlocked.Increment(ref _mrtrInFlightCount);
			ObserveHandlerCompletionAsync(handlerTask);
			return await AwaitMrtrHandlerAsync(handlerTask, continuation, mrtrContext.InitialExchangeTask, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		};
	}

	/// <summary>
	/// Awaits the outcome of an MRTR-enabled handler invocation.
	/// If the handler completes, returns its result. If an exchange arrives (handler needs input),
	/// builds and returns an <see cref="T:ModelContextProtocol.Protocol.InputRequiredResult" /> and stores the continuation for future retries.
	/// If the handler throws <see cref="T:ModelContextProtocol.Protocol.InputRequiredException" />, the result is returned directly
	/// without storing a continuation (explicit MRTR path).
	/// </summary>
	private async Task<JsonNode?> AwaitMrtrHandlerAsync(Task<JsonNode?> handlerTask, MrtrContinuation continuation, Task<MrtrExchange> exchangeTask, CancellationToken cancellationToken)
	{
		using (cancellationToken.Register(delegate(object? state)
		{
			((MrtrContinuation)state).CancelHandler();
		}, continuation))
		{
			if (await Task.WhenAny(handlerTask, exchangeTask).ConfigureAwait(continueOnCapturedContext: false) == handlerTask)
			{
				return await AwaitHandlerWithInputRequiredResultHandlingAsync(handlerTask).ConfigureAwait(continueOnCapturedContext: false);
			}
			MrtrExchange mrtrExchange = await exchangeTask.ConfigureAwait(continueOnCapturedContext: false);
			string text = Guid.NewGuid().ToString("N");
			InputRequiredResult inputRequiredResult = new InputRequiredResult
			{
				InputRequests = new Dictionary<string, InputRequest> { [mrtrExchange.Key] = mrtrExchange.InputRequest },
				RequestState = text
			};
			continuation.PendingExchange = mrtrExchange;
			_mrtrContinuations[text] = continuation;
			return SerializeInputRequiredResult(inputRequiredResult);
		}
	}

	/// <summary>
	/// Fire-and-forget observer for an MRTR handler task. Logs unhandled exceptions at Debug
	/// level (the same exception still propagates to the request pipeline, so Debug avoids
	/// double-reporting at Error) and decrements <see cref="F:ModelContextProtocol.Server.McpServerImpl._mrtrInFlightCount" /> when the
	/// handler completes, following the same in-flight tracking pattern as <see cref="T:ModelContextProtocol.McpSessionHandler" />.
	/// </summary>
	private async Task ObserveHandlerCompletionAsync(Task<JsonNode?> handlerTask)
	{
		try
		{
			await handlerTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (InputRequiredException)
		{
		}
		catch (Exception exception)
		{
			MrtrHandlerError(exception);
		}
		finally
		{
			if (Interlocked.Decrement(ref _mrtrInFlightCount) == 0)
			{
				_allMrtrHandlersCompleted.TrySetResult(result: true);
			}
		}
	}

	/// <summary>
	/// Awaits a handler task, catching <see cref="T:ModelContextProtocol.Protocol.InputRequiredException" /> to convert it to an
	/// <see cref="T:ModelContextProtocol.Protocol.InputRequiredResult" /> JSON response without storing a continuation.
	/// </summary>
	private static async Task<JsonNode?> AwaitHandlerWithInputRequiredResultHandlingAsync(Task<JsonNode?> handlerTask)
	{
		try
		{
			return await handlerTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (InputRequiredException ex)
		{
			return SerializeInputRequiredResult(ex.Result);
		}
	}

	[LoggerMessage(Level = LogLevel.Error, Message = "\"{ToolName}\" threw an unhandled exception.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void ToolCallError(string toolName, Exception exception)
	{
		if (_logger.IsEnabled(LogLevel.Error))
		{
			__ToolCallErrorCallback(_logger, toolName, exception);
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "\"{ToolName}\" completed. IsError = {IsError}.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void ToolCallCompleted(string toolName, bool isError)
	{
		if (_logger.IsEnabled(LogLevel.Information))
		{
			__ToolCallCompletedCallback(_logger, toolName, isError, null);
		}
	}

	[LoggerMessage(Level = LogLevel.Error, Message = "GetPrompt \"{PromptName}\" threw an unhandled exception.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void GetPromptError(string promptName, Exception exception)
	{
		if (_logger.IsEnabled(LogLevel.Error))
		{
			__GetPromptErrorCallback(_logger, promptName, exception);
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "GetPrompt \"{PromptName}\" completed.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void GetPromptCompleted(string promptName)
	{
		if (_logger.IsEnabled(LogLevel.Information))
		{
			__GetPromptCompletedCallback(_logger, promptName, null);
		}
	}

	[LoggerMessage(Level = LogLevel.Error, Message = "ReadResource \"{ResourceUri}\" threw an unhandled exception.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void ReadResourceError(string resourceUri, Exception exception)
	{
		if (_logger.IsEnabled(LogLevel.Error))
		{
			__ReadResourceErrorCallback(_logger, resourceUri, exception);
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "ReadResource \"{ResourceUri}\" completed.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void ReadResourceCompleted(string resourceUri)
	{
		if (_logger.IsEnabled(LogLevel.Information))
		{
			__ReadResourceCompletedCallback(_logger, resourceUri, null);
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Cancelled {Count} pending MRTR continuation(s) during session disposal.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void MrtrContinuationsCancelled(int count)
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			__MrtrContinuationsCancelledCallback(_logger, count, null);
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "An MRTR handler threw an unhandled exception.")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void MrtrHandlerError(Exception exception)
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			__MrtrHandlerErrorCallback(_logger, exception);
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Failed to deliver \"{NotificationMethod}\" to subscription \"{SubscriptionId}\".")]
	[GeneratedCode("Microsoft.Extensions.Logging.Generators", "10.0.14.32716")]
	private void SubscriptionNotificationFailed(string notificationMethod, string subscriptionId, Exception exception)
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			__SubscriptionNotificationFailedCallback(_logger, notificationMethod, subscriptionId, exception);
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
