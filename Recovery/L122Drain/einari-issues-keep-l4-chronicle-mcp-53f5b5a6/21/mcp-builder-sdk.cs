using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides methods for configuring MCP servers via dependency injection.
/// </summary>
public static class McpServerBuilderExtensions
{
	private const string WithToolsRequiresUnreferencedCodeMessage = "The non-generic WithTools and WithToolsFromAssembly methods require dynamic lookup of method metadata and might not work in Native AOT. Use the generic WithTools method instead.";

	private const string WithPromptsRequiresUnreferencedCodeMessage = "The non-generic WithPrompts and WithPromptsFromAssembly methods require dynamic lookup of method metadata and might not work in Native AOT. Use the generic WithPrompts method instead.";

	private const string WithResourcesRequiresUnreferencedCodeMessage = "The non-generic WithResources and WithResourcesFromAssembly methods require dynamic lookup of member metadata and might not work in Native AOT. Use the generic WithResources method instead.";

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerTool" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <typeparam name="TToolType">The tool type.</typeparam>
	/// <param name="builder">The builder instance.</param>
	/// <param name="serializerOptions">The serializer options governing tool parameter marshalling.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// This method discovers all instance and static methods (public and non-public) on the specified <typeparamref name="TToolType" />
	/// type, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerToolAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerTool" />
	/// instance for each. For instance methods, an instance is constructed for each invocation of the tool.
	/// </remarks>
	public static IMcpServerBuilder WithTools<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TToolType>(this IMcpServerBuilder builder, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		MethodInfo[] methods = typeof(TToolType).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo toolMethod in methods)
		{
			if (((MemberInfo)toolMethod).GetCustomAttribute<McpServerToolAttribute>() == null)
			{
				continue;
			}
			builder.Services.AddSingleton(toolMethod.IsStatic ? ((Func<IServiceProvider, McpServerTool>)((IServiceProvider services) => McpServerTool.Create(toolMethod, (object)null, new McpServerToolCreateOptions
			{
				Services = services,
				SerializerOptions = serializerOptions
			}))) : ((Func<IServiceProvider, McpServerTool>)((IServiceProvider services) => McpServerTool.Create(toolMethod, (Func<RequestContext<CallToolRequestParams>, object>)((RequestContext<CallToolRequestParams> r) => CreateTarget(((MessageContext)r).Services, typeof(TToolType))), new McpServerToolCreateOptions
			{
				Services = services,
				SerializerOptions = serializerOptions
			}))));
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerTool" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <typeparam name="TToolType">The tool type.</typeparam>
	/// <param name="builder">The builder instance.</param>
	/// <param name="target">The target instance from which the tools should be sourced.</param>
	/// <param name="serializerOptions">The serializer options governing tool parameter marshalling.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="target" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This method discovers all methods (public and non-public) on the specified <typeparamref name="TToolType" />
	/// type, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerToolAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerTool" />
	/// instance for each, using <paramref name="target" /> as the associated instance for instance methods.
	/// </para>
	/// <para>
	/// However, if <typeparamref name="TToolType" /> is itself an <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:ModelContextProtocol.Server.McpServerTool" />,
	/// this method registers those tools directly without scanning for methods on <typeparamref name="TToolType" />.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithTools<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TToolType>(this IMcpServerBuilder builder, TToolType target, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(target, "target");
		if (target is IEnumerable<McpServerTool> tools)
		{
			return builder.WithTools(tools);
		}
		MethodInfo[] methods = typeof(TToolType).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo toolMethod in methods)
		{
			if (((MemberInfo)toolMethod).GetCustomAttribute<McpServerToolAttribute>() != null)
			{
				builder.Services.AddSingleton((IServiceProvider services) => McpServerTool.Create(toolMethod, toolMethod.IsStatic ? null : ((object)target), new McpServerToolCreateOptions
				{
					Services = services,
					SerializerOptions = serializerOptions
				}));
			}
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerTool" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="tools">The <see cref="T:ModelContextProtocol.Server.McpServerTool" /> instances to add to the server.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="tools" /> is <see langword="null" />.</exception>
	public static IMcpServerBuilder WithTools(this IMcpServerBuilder builder, IEnumerable<McpServerTool> tools)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(tools, "tools");
		foreach (McpServerTool tool in tools)
		{
			if (tool != null)
			{
				builder.Services.AddSingleton<McpServerTool>(tool);
			}
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerTool" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="toolTypes">Types with <see cref="T:ModelContextProtocol.Server.McpServerToolAttribute" />-attributed methods to add as tools to the server.</param>
	/// <param name="serializerOptions">The serializer options governing tool parameter marshalling.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="toolTypes" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// This method discovers all instance and static methods (public and non-public) on the specified <paramref name="toolTypes" />
	/// types, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerToolAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerTool" />
	/// instance for each. For instance methods, an instance is constructed for each invocation of the tool.
	/// </remarks>
	[RequiresUnreferencedCode("The non-generic WithTools and WithToolsFromAssembly methods require dynamic lookup of method metadata and might not work in Native AOT. Use the generic WithTools method instead.")]
	public static IMcpServerBuilder WithTools(this IMcpServerBuilder builder, IEnumerable<Type> toolTypes, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(toolTypes, "toolTypes");
		foreach (Type toolType in toolTypes)
		{
			if ((object)toolType == null)
			{
				continue;
			}
			MethodInfo[] methods = toolType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo toolMethod in methods)
			{
				if (((MemberInfo)toolMethod).GetCustomAttribute<McpServerToolAttribute>() == null)
				{
					continue;
				}
				builder.Services.AddSingleton(toolMethod.IsStatic ? ((Func<IServiceProvider, McpServerTool>)((IServiceProvider services) => McpServerTool.Create(toolMethod, (object)null, new McpServerToolCreateOptions
				{
					Services = services,
					SerializerOptions = serializerOptions
				}))) : ((Func<IServiceProvider, McpServerTool>)((IServiceProvider services) => McpServerTool.Create(toolMethod, (Func<RequestContext<CallToolRequestParams>, object>)((RequestContext<CallToolRequestParams> r) => CreateTarget(((MessageContext)r).Services, toolType)), new McpServerToolCreateOptions
				{
					Services = services,
					SerializerOptions = serializerOptions
				}))));
			}
		}
		return builder;
	}

	/// <summary>
	/// Adds types marked with the <see cref="T:ModelContextProtocol.Server.McpServerToolTypeAttribute" /> attribute from the given assembly as tools to the server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="serializerOptions">The serializer options governing tool parameter marshalling.</param>
	/// <param name="toolAssembly">The assembly to load the types from. If <see langword="null" />, the calling assembly is used.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This method scans the specified assembly (or the calling assembly if none is provided) for classes
	/// marked with the <see cref="T:ModelContextProtocol.Server.McpServerToolTypeAttribute" />. It then discovers all methods within those
	/// classes that are marked with the <see cref="T:ModelContextProtocol.Server.McpServerToolAttribute" /> and registers them as <see cref="T:ModelContextProtocol.Server.McpServerTool" />s
	/// in the <paramref name="builder" />'s <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
	/// </para>
	/// <para>
	/// The method automatically handles both static and instance methods. For instance methods, a new instance
	/// of the containing class is constructed for each invocation of the tool.
	/// </para>
	/// <para>
	/// Tools registered through this method can be discovered by clients using the <c>list_tools</c> request
	/// and invoked using the <c>call_tool</c> request.
	/// </para>
	/// <para>
	/// Note that this method performs reflection at runtime and might not work in Native AOT scenarios. For
	/// Native AOT compatibility, consider using the generic <see cref="M:WithTools" /> method instead.
	/// </para>
	/// </remarks>
	[RequiresUnreferencedCode("The non-generic WithTools and WithToolsFromAssembly methods require dynamic lookup of method metadata and might not work in Native AOT. Use the generic WithTools method instead.")]
	public static IMcpServerBuilder WithToolsFromAssembly(this IMcpServerBuilder builder, Assembly? toolAssembly = null, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		if ((object)toolAssembly == null)
		{
			toolAssembly = Assembly.GetCallingAssembly();
		}
		return builder.WithTools(from t in toolAssembly.GetTypes()
			where ((MemberInfo)t).GetCustomAttribute<McpServerToolTypeAttribute>() != null
			select t, serializerOptions);
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerPrompt" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <typeparam name="TPromptType">The prompt type.</typeparam>
	/// <param name="builder">The builder instance.</param>
	/// <param name="serializerOptions">The serializer options governing prompt parameter marshalling.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// This method discovers all instance and static methods (public and non-public) on the specified <typeparamref name="TPromptType" />
	/// type, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerPromptAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerPrompt" />
	/// instance for each. For instance methods, an instance is constructed for each invocation of the prompt.
	/// </remarks>
	public static IMcpServerBuilder WithPrompts<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TPromptType>(this IMcpServerBuilder builder, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		MethodInfo[] methods = typeof(TPromptType).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo promptMethod in methods)
		{
			if (((MemberInfo)promptMethod).GetCustomAttribute<McpServerPromptAttribute>() == null)
			{
				continue;
			}
			builder.Services.AddSingleton(promptMethod.IsStatic ? ((Func<IServiceProvider, McpServerPrompt>)((IServiceProvider services) => McpServerPrompt.Create(promptMethod, (object)null, new McpServerPromptCreateOptions
			{
				Services = services,
				SerializerOptions = serializerOptions
			}))) : ((Func<IServiceProvider, McpServerPrompt>)((IServiceProvider services) => McpServerPrompt.Create(promptMethod, (Func<RequestContext<GetPromptRequestParams>, object>)((RequestContext<GetPromptRequestParams> r) => CreateTarget(((MessageContext)r).Services, typeof(TPromptType))), new McpServerPromptCreateOptions
			{
				Services = services,
				SerializerOptions = serializerOptions
			}))));
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerPrompt" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <typeparam name="TPromptType">The prompt type.</typeparam>
	/// <param name="builder">The builder instance.</param>
	/// <param name="target">The target instance from which the prompts should be sourced.</param>
	/// <param name="serializerOptions">The serializer options governing prompt parameter marshalling.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="target" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This method discovers all methods (public and non-public) on the specified <typeparamref name="TPromptType" />
	/// type, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerPromptAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerPrompt" />
	/// instance for each, using <paramref name="target" /> as the associated instance for instance methods.
	/// </para>
	/// <para>
	/// However, if <typeparamref name="TPromptType" /> is itself an <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:ModelContextProtocol.Server.McpServerPrompt" />,
	/// this method registers those prompts directly without scanning for methods on <typeparamref name="TPromptType" />.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithPrompts<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TPromptType>(this IMcpServerBuilder builder, TPromptType target, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(target, "target");
		if (target is IEnumerable<McpServerPrompt> prompts)
		{
			return builder.WithPrompts(prompts);
		}
		MethodInfo[] methods = typeof(TPromptType).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo promptMethod in methods)
		{
			if (((MemberInfo)promptMethod).GetCustomAttribute<McpServerPromptAttribute>() != null)
			{
				builder.Services.AddSingleton((IServiceProvider services) => McpServerPrompt.Create(promptMethod, (object)target, new McpServerPromptCreateOptions
				{
					Services = services,
					SerializerOptions = serializerOptions
				}));
			}
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerPrompt" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="prompts">The <see cref="T:ModelContextProtocol.Server.McpServerPrompt" /> instances to add to the server.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="prompts" /> is <see langword="null" />.</exception>
	public static IMcpServerBuilder WithPrompts(this IMcpServerBuilder builder, IEnumerable<McpServerPrompt> prompts)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(prompts, "prompts");
		foreach (McpServerPrompt prompt in prompts)
		{
			if (prompt != null)
			{
				builder.Services.AddSingleton<McpServerPrompt>(prompt);
			}
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerPrompt" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="promptTypes">Types with marked methods to add as prompts to the server.</param>
	/// <param name="serializerOptions">The serializer options governing prompt parameter marshalling.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="promptTypes" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// This method discovers all instance and static methods (public and non-public) on the specified <paramref name="promptTypes" />
	/// types, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerPromptAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerPrompt" />
	/// instance for each. For instance methods, an instance is constructed for each invocation of the prompt.
	/// </remarks>
	[RequiresUnreferencedCode("The non-generic WithPrompts and WithPromptsFromAssembly methods require dynamic lookup of method metadata and might not work in Native AOT. Use the generic WithPrompts method instead.")]
	public static IMcpServerBuilder WithPrompts(this IMcpServerBuilder builder, IEnumerable<Type> promptTypes, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(promptTypes, "promptTypes");
		foreach (Type promptType in promptTypes)
		{
			if ((object)promptType == null)
			{
				continue;
			}
			MethodInfo[] methods = promptType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo promptMethod in methods)
			{
				if (((MemberInfo)promptMethod).GetCustomAttribute<McpServerPromptAttribute>() == null)
				{
					continue;
				}
				builder.Services.AddSingleton(promptMethod.IsStatic ? ((Func<IServiceProvider, McpServerPrompt>)((IServiceProvider services) => McpServerPrompt.Create(promptMethod, (object)null, new McpServerPromptCreateOptions
				{
					Services = services,
					SerializerOptions = serializerOptions
				}))) : ((Func<IServiceProvider, McpServerPrompt>)((IServiceProvider services) => McpServerPrompt.Create(promptMethod, (Func<RequestContext<GetPromptRequestParams>, object>)((RequestContext<GetPromptRequestParams> r) => CreateTarget(((MessageContext)r).Services, promptType)), new McpServerPromptCreateOptions
				{
					Services = services,
					SerializerOptions = serializerOptions
				}))));
			}
		}
		return builder;
	}

	/// <summary>
	/// Adds types marked with the <see cref="T:ModelContextProtocol.Server.McpServerPromptTypeAttribute" /> attribute from the given assembly as prompts to the server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="serializerOptions">The serializer options governing prompt parameter marshalling.</param>
	/// <param name="promptAssembly">The assembly to load the types from. If <see langword="null" />, the calling assembly is used.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This method scans the specified assembly (or the calling assembly if none is provided) for classes
	/// marked with the <see cref="T:ModelContextProtocol.Server.McpServerPromptTypeAttribute" />. It then discovers all methods within those
	/// classes that are marked with the <see cref="T:ModelContextProtocol.Server.McpServerPromptAttribute" /> and registers them as <see cref="T:ModelContextProtocol.Server.McpServerPrompt" />s
	/// in the <paramref name="builder" />'s <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
	/// </para>
	/// <para>
	/// The method automatically handles both static and instance methods. For instance methods, a new instance
	/// of the containing class is constructed for each invocation of the prompt.
	/// </para>
	/// <para>
	/// Prompts registered through this method can be discovered by clients using the <c>list_prompts</c> request
	/// and invoked using the <c>prompts/get</c> request.
	/// </para>
	/// <para>
	/// Note that this method performs reflection at runtime and might not work in Native AOT scenarios. For
	/// Native AOT compatibility, consider using the generic <see cref="M:WithPrompts" /> method instead.
	/// </para>
	/// </remarks>
	[RequiresUnreferencedCode("The non-generic WithPrompts and WithPromptsFromAssembly methods require dynamic lookup of method metadata and might not work in Native AOT. Use the generic WithPrompts method instead.")]
	public static IMcpServerBuilder WithPromptsFromAssembly(this IMcpServerBuilder builder, Assembly? promptAssembly = null, JsonSerializerOptions? serializerOptions = null)
	{
		Throw.IfNull(builder, "builder");
		if ((object)promptAssembly == null)
		{
			promptAssembly = Assembly.GetCallingAssembly();
		}
		return builder.WithPrompts(from t in promptAssembly.GetTypes()
			where ((MemberInfo)t).GetCustomAttribute<McpServerPromptTypeAttribute>() != null
			select t, serializerOptions);
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerResource" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <typeparam name="TResourceType">The resource type.</typeparam>
	/// <param name="builder">The builder instance.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// This method discovers all instance and static methods (public and non-public) on the specified <typeparamref name="TResourceType" />
	/// type, where the members are attributed as <see cref="T:ModelContextProtocol.Server.McpServerResourceAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerResource" />
	/// instance for each. For instance members, an instance is constructed for each invocation of the resource.
	/// </remarks>
	public static IMcpServerBuilder WithResources<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TResourceType>(this IMcpServerBuilder builder)
	{
		Throw.IfNull(builder, "builder");
		MethodInfo[] methods = typeof(TResourceType).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo resourceTemplateMethod in methods)
		{
			if (((MemberInfo)resourceTemplateMethod).GetCustomAttribute<McpServerResourceAttribute>() == null)
			{
				continue;
			}
			builder.Services.AddSingleton(resourceTemplateMethod.IsStatic ? ((Func<IServiceProvider, McpServerResource>)((IServiceProvider services) => McpServerResource.Create(resourceTemplateMethod, (object)null, new McpServerResourceCreateOptions
			{
				Services = services
			}))) : ((Func<IServiceProvider, McpServerResource>)((IServiceProvider services) => McpServerResource.Create(resourceTemplateMethod, (Func<RequestContext<ReadResourceRequestParams>, object>)((RequestContext<ReadResourceRequestParams> r) => CreateTarget(((MessageContext)r).Services, typeof(TResourceType))), new McpServerResourceCreateOptions
			{
				Services = services
			}))));
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerResource" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <typeparam name="TResourceType">The resource type.</typeparam>
	/// <param name="builder">The builder instance.</param>
	/// <param name="target">The target instance from which the resources should be sourced.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="target" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This method discovers all methods (public and non-public) on the specified <typeparamref name="TResourceType" />
	/// type, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerResourceAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerResource" />
	/// instance for each, using <paramref name="target" /> as the associated instance for instance methods.
	/// </para>
	/// <para>
	/// However, if <typeparamref name="TResourceType" /> is itself an <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:ModelContextProtocol.Server.McpServerResource" />,
	/// this method registers those resources directly without scanning for methods on <typeparamref name="TResourceType" />.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithResources<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TResourceType>(this IMcpServerBuilder builder, TResourceType target)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(target, "target");
		if (target is IEnumerable<McpServerResource> resourceTemplates)
		{
			return builder.WithResources(resourceTemplates);
		}
		MethodInfo[] methods = typeof(TResourceType).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo resourceTemplateMethod in methods)
		{
			if (((MemberInfo)resourceTemplateMethod).GetCustomAttribute<McpServerResourceAttribute>() != null)
			{
				builder.Services.AddSingleton((IServiceProvider services) => McpServerResource.Create(resourceTemplateMethod, (object)target, new McpServerResourceCreateOptions
				{
					Services = services
				}));
			}
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerResource" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="resourceTemplates">The <see cref="T:ModelContextProtocol.Server.McpServerResource" /> instances to add to the server.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="resourceTemplates" /> is <see langword="null" />.</exception>
	public static IMcpServerBuilder WithResources(this IMcpServerBuilder builder, IEnumerable<McpServerResource> resourceTemplates)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(resourceTemplates, "resourceTemplates");
		foreach (McpServerResource resourceTemplate in resourceTemplates)
		{
			if (resourceTemplate != null)
			{
				builder.Services.AddSingleton<McpServerResource>(resourceTemplate);
			}
		}
		return builder;
	}

	/// <summary>Adds <see cref="T:ModelContextProtocol.Server.McpServerResource" /> instances to the service collection backing <paramref name="builder" />.</summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="resourceTemplateTypes">Types with marked methods to add as resources to the server.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="resourceTemplateTypes" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// This method discovers all instance and static methods (public and non-public) on the specified <paramref name="resourceTemplateTypes" />
	/// types, where the methods are attributed as <see cref="T:ModelContextProtocol.Server.McpServerResourceAttribute" />, and adds an <see cref="T:ModelContextProtocol.Server.McpServerResource" />
	/// instance for each. For instance methods, an instance is constructed for each invocation of the resource.
	/// </remarks>
	[RequiresUnreferencedCode("The non-generic WithResources and WithResourcesFromAssembly methods require dynamic lookup of member metadata and might not work in Native AOT. Use the generic WithResources method instead.")]
	public static IMcpServerBuilder WithResources(this IMcpServerBuilder builder, IEnumerable<Type> resourceTemplateTypes)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(resourceTemplateTypes, "resourceTemplateTypes");
		foreach (Type resourceTemplateType in resourceTemplateTypes)
		{
			if ((object)resourceTemplateType == null)
			{
				continue;
			}
			MethodInfo[] methods = resourceTemplateType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo resourceTemplateMethod in methods)
			{
				if (((MemberInfo)resourceTemplateMethod).GetCustomAttribute<McpServerResourceAttribute>() == null)
				{
					continue;
				}
				builder.Services.AddSingleton(resourceTemplateMethod.IsStatic ? ((Func<IServiceProvider, McpServerResource>)((IServiceProvider services) => McpServerResource.Create(resourceTemplateMethod, (object)null, new McpServerResourceCreateOptions
				{
					Services = services
				}))) : ((Func<IServiceProvider, McpServerResource>)((IServiceProvider services) => McpServerResource.Create(resourceTemplateMethod, (Func<RequestContext<ReadResourceRequestParams>, object>)((RequestContext<ReadResourceRequestParams> r) => CreateTarget(((MessageContext)r).Services, resourceTemplateType)), new McpServerResourceCreateOptions
				{
					Services = services
				}))));
			}
		}
		return builder;
	}

	/// <summary>
	/// Adds types marked with the <see cref="T:ModelContextProtocol.Server.McpServerResourceTypeAttribute" /> attribute from the given assembly as resources to the server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="resourceAssembly">The assembly to load the types from. If <see langword="null" />, the calling assembly is used.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This method scans the specified assembly (or the calling assembly if none is provided) for classes
	/// marked with the <see cref="T:ModelContextProtocol.Server.McpServerResourceTypeAttribute" />. It then discovers all members within those
	/// classes that are marked with the <see cref="T:ModelContextProtocol.Server.McpServerResourceAttribute" /> and registers them as <see cref="T:ModelContextProtocol.Server.McpServerResource" />s
	/// in the <paramref name="builder" />'s <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
	/// </para>
	/// <para>
	/// The method automatically handles both static and instance members. For instance members, a new instance
	/// of the containing class is constructed for each invocation of the resource.
	/// </para>
	/// <para>
	/// Resource templates registered through this method can be discovered by clients using the <c>list_resourceTemplates</c> request
	/// and invoked using the <c>read_resource</c> request.
	/// </para>
	/// <para>
	/// Note that this method performs reflection at runtime and might not work in Native AOT scenarios. For
	/// Native AOT compatibility, consider using the generic <see cref="M:WithResources" /> method instead.
	/// </para>
	/// </remarks>
	[RequiresUnreferencedCode("The non-generic WithResources and WithResourcesFromAssembly methods require dynamic lookup of member metadata and might not work in Native AOT. Use the generic WithResources method instead.")]
	public static IMcpServerBuilder WithResourcesFromAssembly(this IMcpServerBuilder builder, Assembly? resourceAssembly = null)
	{
		Throw.IfNull(builder, "builder");
		if ((object)resourceAssembly == null)
		{
			resourceAssembly = Assembly.GetCallingAssembly();
		}
		return builder.WithResources(from t in resourceAssembly.GetTypes()
			where ((MemberInfo)t).GetCustomAttribute<McpServerResourceTypeAttribute>() != null
			select t);
	}

	/// <summary>
	/// Configures a handler for listing resource templates available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes resource template list requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This handler is responsible for providing clients with information about available resource templates
	/// that can be used to construct resource URIs.
	/// </para>
	/// <para>
	/// Resource templates describe the structure of resource URIs that the server can handle. They include
	/// URI templates (according to RFC 6570) that clients can use to construct valid resource URIs.
	/// </para>
	/// <para>
	/// This handler is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithReadResourceHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.ReadResourceRequestParams,ModelContextProtocol.Protocol.ReadResourceResult})" /> to provide a complete
	/// resource system where templates define the URI patterns and the read handler provides the actual content.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithListResourceTemplatesHandler(this IMcpServerBuilder builder, McpRequestHandler<ListResourceTemplatesRequestParams, ListResourceTemplatesResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.ListResourceTemplatesHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Resources == null)
			{
				ServerCapabilities obj = val2;
				ResourcesCapability val3 = new ResourcesCapability();
				ResourcesCapability val4 = val3;
				obj.Resources = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for listing tools available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler that processes list tools requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This handler is called when a client requests a list of available tools. It should return all tools
	/// that can be invoked through the server, including their names, descriptions, and parameter specifications.
	/// The handler can optionally support pagination via the cursor mechanism for large or dynamically generated
	/// tool collections.
	/// </para>
	/// <para>
	/// When tools are also defined using <see cref="T:ModelContextProtocol.Server.McpServerTool" /> collection, both sets of tools
	/// will be combined in the response to clients. This allows for a mix of programmatically defined
	/// tools and dynamically generated tools.
	/// </para>
	/// <para>
	/// This method is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithCallToolHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.CallToolRequestParams,ModelContextProtocol.Protocol.CallToolResult})" /> to provide a complete tools implementation,
	/// where <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithListToolsHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.ListToolsRequestParams,ModelContextProtocol.Protocol.ListToolsResult})" /> advertises available tools and <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithCallToolHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.CallToolRequestParams,ModelContextProtocol.Protocol.CallToolResult})" />
	/// executes them when invoked by clients.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithListToolsHandler(this IMcpServerBuilder builder, McpRequestHandler<ListToolsRequestParams, ListToolsResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.ListToolsHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Tools == null)
			{
				ServerCapabilities obj = val2;
				ToolsCapability val3 = new ToolsCapability();
				ToolsCapability val4 = val3;
				obj.Tools = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for calling tools available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes tool calls.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// The call tool handler is responsible for executing custom tools and returning their results to clients.
	/// This method is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithListToolsHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.ListToolsRequestParams,ModelContextProtocol.Protocol.ListToolsResult})" /> to provide a complete tools implementation,
	/// where <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithListToolsHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.ListToolsRequestParams,ModelContextProtocol.Protocol.ListToolsResult})" /> advertises available tools and this handler executes them.
	/// </remarks>
	public static IMcpServerBuilder WithCallToolHandler(this IMcpServerBuilder builder, McpRequestHandler<CallToolRequestParams, CallToolResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.CallToolHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Tools == null)
			{
				ServerCapabilities obj = val2;
				ToolsCapability val3 = new ToolsCapability();
				ToolsCapability val4 = val3;
				obj.Tools = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for listing prompts available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler that processes list prompts requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This handler is called when a client requests a list of available prompts. It should return all prompts
	/// that can be invoked through the server, including their names, descriptions, and parameter specifications.
	/// The handler can optionally support pagination via the cursor mechanism for large or dynamically-generated
	/// prompt collections.
	/// </para>
	/// <para>
	/// When prompts are also defined using <see cref="T:ModelContextProtocol.Server.McpServerPrompt" /> collection, both sets of prompts
	/// will be combined in the response to clients. This allows for a mix of programmatically defined
	/// prompts and dynamically generated prompts.
	/// </para>
	/// <para>
	/// This method is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithGetPromptHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.GetPromptRequestParams,ModelContextProtocol.Protocol.GetPromptResult})" /> to provide a complete prompts implementation,
	/// where <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithListPromptsHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.ListPromptsRequestParams,ModelContextProtocol.Protocol.ListPromptsResult})" /> advertises available prompts and <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithGetPromptHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.GetPromptRequestParams,ModelContextProtocol.Protocol.GetPromptResult})" />
	/// produces them when invoked by clients.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithListPromptsHandler(this IMcpServerBuilder builder, McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.ListPromptsHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Prompts == null)
			{
				ServerCapabilities obj = val2;
				PromptsCapability val3 = new PromptsCapability();
				PromptsCapability val4 = val3;
				obj.Prompts = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for getting a prompt available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes prompt requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	public static IMcpServerBuilder WithGetPromptHandler(this IMcpServerBuilder builder, McpRequestHandler<GetPromptRequestParams, GetPromptResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.GetPromptHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Prompts == null)
			{
				ServerCapabilities obj = val2;
				PromptsCapability val3 = new PromptsCapability();
				PromptsCapability val4 = val3;
				obj.Prompts = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for listing resources available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes resource list requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This handler is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithReadResourceHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.ReadResourceRequestParams,ModelContextProtocol.Protocol.ReadResourceResult})" /> to provide a complete resources implementation,
	/// where this handler advertises available resources and the read handler provides their content when requested.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithListResourcesHandler(this IMcpServerBuilder builder, McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.ListResourcesHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Resources == null)
			{
				ServerCapabilities obj = val2;
				ResourcesCapability val3 = new ResourcesCapability();
				ResourcesCapability val4 = val3;
				obj.Resources = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for reading a resource available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes resource read requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// This handler is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithListResourcesHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.ListResourcesRequestParams,ModelContextProtocol.Protocol.ListResourcesResult})" /> to provide a complete resources implementation,
	/// where the list handler advertises available resources and the read handler provides their content when requested.
	/// </remarks>
	public static IMcpServerBuilder WithReadResourceHandler(this IMcpServerBuilder builder, McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.ReadResourceHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Resources == null)
			{
				ServerCapabilities obj = val2;
				ResourcesCapability val3 = new ResourcesCapability();
				ResourcesCapability val4 = val3;
				obj.Resources = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for auto-completion suggestions for prompt arguments or resource references available from the Model Context Protocol server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes completion requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// The completion handler is invoked when clients request suggestions for argument values.
	/// This enables auto-complete functionality for both prompt arguments and resource references.
	/// </remarks>
	public static IMcpServerBuilder WithCompleteHandler(this IMcpServerBuilder builder, McpRequestHandler<CompleteRequestParams, CompleteResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.CompleteHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Completions == null)
			{
				ServerCapabilities obj = val2;
				CompletionsCapability val3 = new CompletionsCapability();
				CompletionsCapability val4 = val3;
				obj.Completions = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for resource subscription requests.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes resource subscription requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// The subscribe handler is responsible for registering client interest in specific resources. When a resource
	/// changes, the server can notify all subscribed clients about the change.
	/// </para>
	/// <para>
	/// This handler is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithUnsubscribeFromResourcesHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.UnsubscribeRequestParams,ModelContextProtocol.Protocol.EmptyResult})" /> to provide a complete
	/// subscription management system. Resource subscriptions allow clients to maintain up-to-date information without
	/// needing to poll resources constantly.
	/// </para>
	/// <para>
	/// After registering a subscription, it's the server's responsibility to track which client is subscribed to which
	/// resources and to send appropriate notifications through the connection when resources change.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithSubscribeToResourcesHandler(this IMcpServerBuilder builder, McpRequestHandler<SubscribeRequestParams, EmptyResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.SubscribeToResourcesHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Resources == null)
			{
				ServerCapabilities obj = val2;
				ResourcesCapability val3 = new ResourcesCapability();
				ResourcesCapability val4 = val3;
				obj.Resources = val3;
			}
			options.Capabilities.Resources.Subscribe = true;
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for resource unsubscription requests.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="handler">The handler function that processes resource unsubscription requests.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// The unsubscribe handler is responsible for removing client interest in specific resources. When a client
	/// no longer needs to receive notifications about resource changes, it can send an unsubscribe request.
	/// </para>
	/// <para>
	/// This handler is typically paired with <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithSubscribeToResourcesHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.SubscribeRequestParams,ModelContextProtocol.Protocol.EmptyResult})" /> to provide a complete
	/// subscription management system. The unsubscribe operation is idempotent, meaning it can be called multiple
	/// times for the same resource without causing errors, even if there is no active subscription.
	/// </para>
	/// <para>
	/// After removing a subscription, the server should stop sending notifications to the client about changes
	/// to the specified resource.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithUnsubscribeFromResourcesHandler(this IMcpServerBuilder builder, McpRequestHandler<UnsubscribeRequestParams, EmptyResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.UnsubscribeFromResourcesHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Resources == null)
			{
				ServerCapabilities obj = val2;
				ResourcesCapability val3 = new ResourcesCapability();
				ResourcesCapability val4 = val3;
				obj.Resources = val3;
			}
			options.Capabilities.Resources.Subscribe = true;
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for <c>subscriptions/listen</c> requests (SEP-2575), taking over the long-lived
	/// subscription stream introduced by the 2026-07-28 protocol revision.
	/// </summary>
	/// <param name="builder">The server builder instance.</param>
	/// <param name="handler">The handler that owns the subscription stream for the lifetime of the request.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// <c>subscriptions/listen</c> is a long-lived request whose held-open response is a solicited
	/// server-to-client stream. Providing a handler here lets a server author own that stream to implement
	/// custom subscription kinds, application-driven <c>resources/updated</c> delivery, or subscriptions backed
	/// by their own event source. It is especially useful for stateless Streamable HTTP, where unsolicited
	/// notifications are dropped but the listen request's response stream can still carry notifications for the
	/// duration of the request.
	/// </para>
	/// <para>
	/// This is a full replacement for the SDK's built-in <c>subscriptions/listen</c> handling. When set, the
	/// handler alone is responsible for sending exactly one
	/// <see cref="F:ModelContextProtocol.Protocol.NotificationMethods.SubscriptionsAcknowledgedNotification" /> before any events, tagging every
	/// streamed notification with the listen request id under <c>_meta[<see cref="F:ModelContextProtocol.Protocol.MetaKeys.SubscriptionId" />]</c>,
	/// staying active until the supplied <see cref="T:System.Threading.CancellationToken" /> is cancelled, and returning
	/// <see cref="T:ModelContextProtocol.Protocol.EmptyResult" /> when it completes. See <see cref="P:ModelContextProtocol.Server.McpServerHandlers.SubscriptionsListenHandler" />
	/// for the full contract.
	/// </para>
	/// <para>
	/// Unlike <see cref="M:Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithSubscribeToResourcesHandler(Microsoft.Extensions.DependencyInjection.IMcpServerBuilder,ModelContextProtocol.Server.McpRequestHandler{ModelContextProtocol.Protocol.SubscribeRequestParams,ModelContextProtocol.Protocol.EmptyResult})" />, this method intentionally does not advertise any
	/// server capabilities on the author's behalf. The set of notifications a listen handler honors is decided
	/// at runtime and can include custom kinds not represented by a capability flag, so the author must
	/// configure only the capabilities their handler will actually deliver. Advertised capabilities must match
	/// what the handler delivers.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithSubscriptionsListenHandler(this IMcpServerBuilder builder, McpRequestHandler<SubscriptionsListenRequestParams, EmptyResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			options.Handlers.SubscriptionsListenHandler = handler;
		});
		return builder;
	}

	/// <summary>
	/// Configures a handler for processing logging level change requests from clients.
	/// </summary>
	/// <param name="builder">The server builder instance.</param>
	/// <param name="handler">The handler that processes requests to change the logging level.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// When a client sends a <c>logging/setLevel</c> request, this handler will be invoked to process
	/// the requested level change. The server typically adjusts its internal logging level threshold
	/// and might begin sending log messages at or above the specified level to the client.
	/// </para>
	/// <para>
	/// Regardless of whether a handler is provided, an <see cref="T:ModelContextProtocol.Server.McpServer" /> should itself handle
	/// such notifications by updating its <see cref="P:ModelContextProtocol.Server.McpServer.LoggingLevel" /> property to return the
	/// most recently set level.
	/// </para>
	/// </remarks>
	[Obsolete("The Logging feature is deprecated as of specification version 2026-07-28 and may be removed in a future version. See SEP-2577 for more information.", DiagnosticId = "MCP9005", UrlFormat = "https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/list-of-diagnostics.md#obsolete-apis")]
	public static IMcpServerBuilder WithSetLoggingLevelHandler(this IMcpServerBuilder builder, McpRequestHandler<SetLevelRequestParams, EmptyResult> handler)
	{
		Throw.IfNull(builder, "builder");
		builder.Services.Configure(delegate(McpServerOptions options)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_0028: Expected O, but got Unknown
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected O, but got Unknown
			//IL_0044: Expected O, but got Unknown
			options.Handlers.SetLoggingLevelHandler = handler;
			ServerCapabilities val2;
			if (options.Capabilities == null)
			{
				ServerCapabilities val = new ServerCapabilities();
				val2 = val;
				options.Capabilities = val;
			}
			val2 = options.Capabilities;
			if (val2.Logging == null)
			{
				ServerCapabilities obj = val2;
				LoggingCapability val3 = new LoggingCapability();
				LoggingCapability val4 = val3;
				obj.Logging = val3;
			}
		});
		return builder;
	}

	/// <summary>
	/// Configures message-level filters for the MCP server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="configure">A callback used to register message filters.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
	public static IMcpServerBuilder WithMessageFilters(this IMcpServerBuilder builder, Action<IMcpMessageFilterBuilder> configure)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(configure, "configure");
		configure(new DefaultMcpMessageFilterBuilder(builder));
		return builder;
	}

	/// <summary>
	/// Configures request-specific filters for the MCP server.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="configure">A callback used to register request-specific filters.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
	public static IMcpServerBuilder WithRequestFilters(this IMcpServerBuilder builder, Action<IMcpRequestFilterBuilder> configure)
	{
		Throw.IfNull(builder, "builder");
		Throw.IfNull(configure, "configure");
		configure(new DefaultMcpRequestFilterBuilder(builder));
		return builder;
	}

	/// <summary>
	/// Adds a server transport that uses standard input (stdin) and standard output (stdout) for communication.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
	/// <remarks>
	/// <para>
	/// This method configures the server to communicate using the standard input and output streams,
	/// which is commonly used when the Model Context Protocol server is launched locally by a client process.
	/// </para>
	/// <para>
	/// When using this transport, the server runs as a single-session service that exits when the
	/// stdin stream is closed. This makes it suitable for scenarios where the server should terminate
	/// when the parent process disconnects.
	/// </para>
	/// </remarks>
	public static IMcpServerBuilder WithStdioServerTransport(this IMcpServerBuilder builder)
	{
		Throw.IfNull(builder, "builder");
		AddSingleSessionServerDependencies(builder.Services);
		builder.Services.AddSingleton(delegate(IServiceProvider sp)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Expected O, but got Unknown
			IOptions<McpServerOptions> requiredService = sp.GetRequiredService<IOptions<McpServerOptions>>();
			ILoggerFactory service = sp.GetService<ILoggerFactory>();
			return (ITransport)new StdioServerTransport(requiredService.Value, service);
		});
		return builder;
	}

	/// <summary>
	/// Adds a server transport that uses the specified input and output streams for communication.
	/// </summary>
	/// <param name="builder">The builder instance.</param>
	/// <param name="inputStream">The input <see cref="T:System.IO.Stream" /> to use as standard input.</param>
	/// <param name="outputStream">The output <see cref="T:System.IO.Stream" /> to use as standard output.</param>
	/// <returns>The builder provided in <paramref name="builder" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="inputStream" /> or <paramref name="outputStream" /> is <see langword="null" />.</exception>
	public static IMcpServerBuilder WithStreamServerTransport(this IMcpServerBuilder builder, Stream inputStream, Stream outputStream)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		Throw.IfNull(builder, "builder");
		Throw.IfNull(inputStream, "inputStream");
		Throw.IfNull(outputStream, "outputStream");
		AddSingleSessionServerDependencies(builder.Services);
		builder.Services.AddSingleton<ITransport>((ITransport)new StreamServerTransport(inputStream, outputStream, (string)null, (ILoggerFactory)null));
		return builder;
	}

	private static void AddSingleSessionServerDependencies(IServiceCollection services)
	{
		services.AddHostedService<SingleSessionMcpServerHostedService>();
		services.TryAddSingleton(delegate(IServiceProvider serviceProvider)
		{
			ITransport requiredService = serviceProvider.GetRequiredService<ITransport>();
			IOptions<McpServerOptions> requiredService2 = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>();
			ILoggerFactory service = serviceProvider.GetService<ILoggerFactory>();
			return McpServer.Create(requiredService, requiredService2.Value, service, serviceProvider);
		});
	}

	/// <summary>Creates an instance of the target object.</summary>
	private static object CreateTarget(IServiceProvider? services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
	{
		if (services == null)
		{
			return Activator.CreateInstance(type);
		}
		return ActivatorUtilities.CreateInstance(services, type);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
