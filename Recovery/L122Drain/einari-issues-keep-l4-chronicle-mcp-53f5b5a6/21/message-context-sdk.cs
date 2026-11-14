using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server;

/// <summary>
/// Provides a context container that provides access to the server and resources for processing a JSON-RPC message.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="T:ModelContextProtocol.Server.MessageContext" /> encapsulates contextual information for handling any JSON-RPC message,
/// including requests, responses, notifications, and errors. This is the base class for
/// <see cref="T:ModelContextProtocol.Server.RequestContext`1" />, which adds request-specific properties.
/// </para>
/// <para>
/// This type is typically received as a parameter in message filter delegates registered via
/// <see cref="P:ModelContextProtocol.Server.McpServerFilters.Message" />'s <see cref="P:ModelContextProtocol.Server.McpMessageFilters.IncomingFilters" /> or
/// <see cref="P:ModelContextProtocol.Server.McpMessageFilters.OutgoingFilters" /> collections.
/// </para>
/// </remarks>
public class MessageContext
{
	[CompilerGenerated]
	private McpServer <Server>k__BackingField;

	/// <summary>Gets or sets the server with which this instance is associated.</summary>
	public McpServer Server
	{
		get
		{
			return <Server>k__BackingField;
		}
		set
		{
			Throw.IfNull(value, "value");
			<Server>k__BackingField = value;
		}
	}

	/// <summary>
	/// Gets or sets a key/value collection that can be used to share data within the scope of this message.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This dictionary is shared with the <see cref="P:ModelContextProtocol.Protocol.JsonRpcMessageContext.Items" /> property
	/// on the underlying <see cref="P:ModelContextProtocol.Server.MessageContext.JsonRpcMessage" />, ensuring that data set in message filters
	/// flows through to request-specific filters and handlers.
	/// </para>
	/// </remarks>
	public IDictionary<string, object?> Items
	{
		get
		{
			JsonRpcMessage jsonRpcMessage = JsonRpcMessage;
			JsonRpcMessageContext jsonRpcMessageContext;
			if (jsonRpcMessage.Context == null)
			{
				jsonRpcMessageContext = (jsonRpcMessage.Context = new JsonRpcMessageContext());
			}
			jsonRpcMessageContext = JsonRpcMessage.Context;
			return jsonRpcMessageContext.Items ?? (jsonRpcMessageContext.Items = new Dictionary<string, object>());
		}
		set
		{
			JsonRpcMessage jsonRpcMessage = JsonRpcMessage;
			if (jsonRpcMessage.Context == null)
			{
				JsonRpcMessageContext jsonRpcMessageContext = (jsonRpcMessage.Context = new JsonRpcMessageContext());
			}
			JsonRpcMessage.Context.Items = value;
		}
	}

	/// <summary>Gets or sets the services associated with this message.</summary>
	/// <remarks>
	/// This provider might not be the same instance stored in <see cref="P:ModelContextProtocol.Server.McpServer.Services" />
	/// if <see cref="P:ModelContextProtocol.Server.McpServerOptions.ScopeRequests" /> was true, in which case this
	/// might be a scoped <see cref="T:System.IServiceProvider" /> derived from the server's
	/// <see cref="P:ModelContextProtocol.Server.McpServer.Services" />.
	/// </remarks>
	public IServiceProvider? Services { get; set; }

	/// <summary>Gets or sets the user associated with this message.</summary>
	/// <remarks>
	/// <para>
	/// This property is backed by the <see cref="P:ModelContextProtocol.Protocol.JsonRpcMessageContext.User" /> property
	/// on the underlying <see cref="P:ModelContextProtocol.Server.MessageContext.JsonRpcMessage" />, ensuring that user information set in message filters
	/// flows through to request-specific filters and handlers.
	/// </para>
	/// </remarks>
	public ClaimsPrincipal? User
	{
		get
		{
			return JsonRpcMessage.Context?.User;
		}
		set
		{
			JsonRpcMessage jsonRpcMessage = JsonRpcMessage;
			if (jsonRpcMessage.Context == null)
			{
				JsonRpcMessageContext jsonRpcMessageContext = (jsonRpcMessage.Context = new JsonRpcMessageContext());
			}
			JsonRpcMessage.Context.User = value;
		}
	}

	/// <summary>
	/// Gets the JSON-RPC message associated with this context.
	/// </summary>
	/// <remarks>
	/// This property provides access to the complete JSON-RPC message,
	/// including the method name (for requests/notifications), request ID (for requests/responses),
	/// and associated transport and user information.
	/// </remarks>
	public JsonRpcMessage JsonRpcMessage { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:ModelContextProtocol.Server.MessageContext" /> class with the specified server and JSON-RPC message.
	/// </summary>
	/// <param name="server">The server with which this instance is associated.</param>
	/// <param name="jsonRpcMessage">The JSON-RPC message associated with this context.</param>
	public MessageContext(McpServer server, JsonRpcMessage jsonRpcMessage)
	{
		Throw.IfNull(server, "server");
		Throw.IfNull(jsonRpcMessage, "jsonRpcMessage");
		Server = server;
		JsonRpcMessage = jsonRpcMessage;
		Services = server.Services;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
