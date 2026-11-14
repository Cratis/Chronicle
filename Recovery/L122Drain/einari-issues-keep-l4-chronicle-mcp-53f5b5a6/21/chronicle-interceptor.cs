using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an interceptor that adds authentication headers to gRPC calls.
/// </summary>
/// <param name="tokenProvider">The token provider for obtaining access tokens.</param>
/// <param name="logger">Logger for logging.</param>
public class AuthenticationClientInterceptor(ITokenProvider tokenProvider, ILogger<AuthenticationClientInterceptor> logger) : Interceptor()
{
	/// <summary>
	/// The metadata key for the authorization header.
	/// </summary>
	public const string AuthorizationMetadataKey = "authorization";

	/// <inheritdoc />
	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		ClientInterceptorContext<TRequest, TResponse> val = AddAuthorization<TRequest, TResponse>(context);
		AsyncUnaryCall<TResponse> val2 = continuation.Invoke(request, val);
		return new AsyncUnaryCall<TResponse>(HandleResponseAsync(val2.ResponseAsync, request, val, continuation), val2.ResponseHeadersAsync, (Func<Status>)val2.GetStatus, (Func<Metadata>)val2.GetTrailers, (Action)val2.Dispose);
	}

	/// <inheritdoc />
	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return continuation.Invoke(AddAuthorization<TRequest, TResponse>(context));
	}

	/// <inheritdoc />
	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		return continuation.Invoke(request, AddAuthorization<TRequest, TResponse>(context));
	}

	/// <inheritdoc />
	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return continuation.Invoke(AddAuthorization<TRequest, TResponse>(context));
	}

	/// <inheritdoc />
	public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		return continuation.Invoke(request, AddAuthorization<TRequest, TResponse>(context));
	}

	private async Task<TResponse> HandleResponseAsync<TRequest, TResponse>(Task<TResponse> responseTask, TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation) where TRequest : class where TResponse : class
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return await responseTask;
		}
		catch (object obj) when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			object obj2 = ((obj is RpcException) ? obj : null);
			return obj2 != null && (int)((RpcException)obj2).StatusCode == 16;
		}).Invoke())
		{
			logger.AuthenticationFailedRetryingWithTokenRefresh(context.Method.FullName);
			try
			{
				await tokenProvider.Refresh();
				ClientInterceptorContext<TRequest, TResponse> val = AddAuthorization<TRequest, TResponse>(context);
				AsyncUnaryCall<TResponse> obj3 = continuation.Invoke(request, val);
				logger.RetryingCallAfterTokenRefresh(context.Method.FullName);
				return await obj3.ResponseAsync;
			}
			catch (Exception ex)
			{
				logger.RetryAfterTokenRefreshFailed(context.Method.FullName, ex);
				throw;
			}
		}
	}

	private ClientInterceptorContext<TRequest, TResponse> AddAuthorization<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class
	{
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string result = tokenProvider.GetAccessToken().GetAwaiter().GetResult();
			if (string.IsNullOrEmpty(result))
			{
				logger.NoTokenAvailable(context.Method.FullName);
				return context;
			}
			logger.AddingAuthenticationHeader(context.Method.FullName);
			Metadata val = new Metadata();
			CallOptions options = context.Options;
			if (((CallOptions)(ref options)).Headers != null)
			{
				options = context.Options;
				foreach (Entry header in ((CallOptions)(ref options)).Headers)
				{
					val.Add(header);
				}
			}
			val.Add("authorization", "Bearer " + result);
			Method<TRequest, TResponse> method = context.Method;
			string host = context.Host;
			options = context.Options;
			return new ClientInterceptorContext<TRequest, TResponse>(method, host, ((CallOptions)(ref options)).WithHeaders(val));
		}
		catch (Exception ex)
		{
			logger.FailedToObtainToken(context.Method.FullName, ex);
			return context;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
