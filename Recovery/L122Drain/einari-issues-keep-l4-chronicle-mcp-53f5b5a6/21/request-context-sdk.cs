using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server;

/// <summary>
/// Provides a context container that provides access to the client request parameters and resources for the request.
/// </summary>
/// <typeparam name="TParams">Type of the request parameters specific to each MCP operation.</typeparam>
/// <remarks>
/// The <see cref="T:ModelContextProtocol.Server.RequestContext`1" /> encapsulates all contextual information for handling an MCP request.
/// This type is typically received as a parameter in handler delegates registered with IMcpServerBuilder,
/// and can be injected as parameters into <see cref="T:ModelContextProtocol.Server.McpServerTool" />s.
/// </remarks>
public sealed class RequestContext<TParams> : MessageContext
{
	/// <summary>Gets or sets the parameters associated with this request.</summary>
	public TParams Params { get; set; }

	/// <summary>
	/// Gets or sets the primitive that matched the request.
	/// </summary>
	public IMcpServerPrimitive? MatchedPrimitive { get; set; }

	/// <summary>
	/// Gets the JSON-RPC request associated with this context.
	/// </summary>
	/// <remarks>
	/// This property provides access to the complete JSON-RPC request that initiated this handler invocation,
	/// including the method name, parameters, request ID, and associated transport and user information.
	/// </remarks>
	public JsonRpcRequest JsonRpcRequest
	{
		get
		{
			return (JsonRpcRequest)base.JsonRpcMessage;
		}
		set
		{
			base.JsonRpcMessage = value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:ModelContextProtocol.Server.RequestContext`1" /> class with the specified server, JSON-RPC request, and request parameters.
	/// </summary>
	/// <param name="server">The server with which this instance is associated.</param>
	/// <param name="jsonRpcRequest">The JSON-RPC request associated with this context.</param>
	/// <param name="parameters">The parameters associated with this request.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="server" /> or <paramref name="jsonRpcRequest" /> is <see langword="null" />.</exception>
	public RequestContext(McpServer server, JsonRpcRequest jsonRpcRequest, TParams parameters)
		: base(server, jsonRpcRequest)
	{
		Params = parameters;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:ModelContextProtocol.Server.RequestContext`1" /> class with the specified server and JSON-RPC request.
	/// </summary>
	/// <param name="server">The server with which this instance is associated.</param>
	/// <param name="jsonRpcRequest">The JSON-RPC request associated with this context.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="server" /> or <paramref name="jsonRpcRequest" /> is <see langword="null" />.</exception>
	[Obsolete("Use the constructor overload that accepts a parameters argument.", DiagnosticId = "MCP9003", UrlFormat = "https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/list-of-diagnostics.md#obsolete-apis")]
	public RequestContext(McpServer server, JsonRpcRequest jsonRpcRequest)
		: base(server, jsonRpcRequest)
	{
		Params = default(TParams);
	}

	/// <summary>
	/// Ends the current response and enables polling for updates from the server.
	/// </summary>
	/// <param name="retryInterval">The interval at which the client should poll for updates.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A <see cref="T:System.Threading.Tasks.ValueTask" /> that completes when polling has been enabled.</returns>
	/// <exception cref="T:System.InvalidOperationException">Thrown when the transport does not support polling.</exception>
	public async ValueTask EnablePollingAsync(TimeSpan retryInterval, CancellationToken cancellationToken = default(CancellationToken))
	{
		await ((JsonRpcRequest.Context?.RelatedTransport as StreamableHttpPostTransport) ?? throw new InvalidOperationException("Polling is only supported for Streamable HTTP transports.")).EnablePollingAsync(retryInterval, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
