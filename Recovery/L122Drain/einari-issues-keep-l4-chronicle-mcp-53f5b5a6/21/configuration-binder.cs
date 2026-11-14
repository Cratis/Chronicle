using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Static helper class that allows binding strongly typed objects to configuration values.
/// </summary>
public static class ConfigurationBinder
{
	private const BindingFlags DeclaredOnlyLookup = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	private const string DynamicCodeWarningMessage = "Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.";

	private const string TrimmingWarningMessage = "In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.";

	private const string InstanceGetTypeTrimmingWarningMessage = "Cannot statically analyze the type of instance so its members may be trimmed";

	private const string PropertyTrimmingWarningMessage = "Cannot statically analyze property.PropertyType so its members may be trimmed.";

	/// <summary>
	/// Attempts to bind the configuration instance to a new instance of type T.
	/// If this configuration section has a value, that will be used.
	/// Otherwise binding by matching property names against configuration keys recursively.
	/// </summary>
	/// <typeparam name="T">The type of the new instance to bind.</typeparam>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <returns>The new instance of T if successful, default(T) otherwise.</returns>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	public static T? Get<T>(this IConfiguration configuration)
	{
		return configuration.Get<T>(null);
	}

	/// <summary>
	/// Attempts to bind the configuration instance to a new instance of type T.
	/// If this configuration section has a value, that will be used.
	/// Otherwise binding by matching property names against configuration keys recursively.
	/// </summary>
	/// <typeparam name="T">The type of the new instance to bind.</typeparam>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <param name="configureOptions">Configures the binder options.</param>
	/// <returns>The new instance of T if successful, default(T) otherwise.</returns>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	public static T? Get<T>(this IConfiguration configuration, Action<BinderOptions>? configureOptions)
	{
		ArgumentNullException.ThrowIfNull(configuration, "configuration");
		object obj = configuration.Get(typeof(T), configureOptions);
		if (obj == null)
		{
			return default(T);
		}
		return (T)obj;
	}

	/// <summary>
	/// Attempts to bind the configuration instance to a new instance of type T.
	/// If this configuration section has a value, that will be used.
	/// Otherwise binding by matching property names against configuration keys recursively.
	/// </summary>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <param name="type">The type of the new instance to bind.</param>
	/// <returns>The new instance if successful, null otherwise.</returns>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	public static object? Get(this IConfiguration configuration, Type type)
	{
		return configuration.Get(type, null);
	}

	/// <summary>
	/// Attempts to bind the configuration instance to a new instance of type T.
	/// If this configuration section has a value, that will be used.
	/// Otherwise binding by matching property names against configuration keys recursively.
	/// </summary>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <param name="type">The type of the new instance to bind.</param>
	/// <param name="configureOptions">Configures the binder options.</param>
	/// <returns>The new instance if successful, null otherwise.</returns>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	public static object? Get(this IConfiguration configuration, Type type, Action<BinderOptions>? configureOptions)
	{
		ArgumentNullException.ThrowIfNull(configuration, "configuration");
		ArgumentNullException.ThrowIfNull(type, "type");
		BinderOptions binderOptions = new BinderOptions();
		configureOptions?.Invoke(binderOptions);
		BindingPoint bindingPoint = new BindingPoint();
		BindInstance(type, bindingPoint, configuration, binderOptions, isParentCollection: false);
		return bindingPoint.Value;
	}

	/// <summary>
	/// Attempts to bind the given object instance to the configuration section specified by the key by matching property names against configuration keys recursively.
	/// </summary>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <param name="key">The key of the configuration section to bind.</param>
	/// <param name="instance">The object to bind.</param>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze the type of instance so its members may be trimmed")]
	public static void Bind(this IConfiguration configuration, string key, object? instance)
	{
		ArgumentNullException.ThrowIfNull(configuration, "configuration");
		configuration.GetSection(key).Bind(instance);
	}

	/// <summary>
	/// Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.
	/// </summary>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <param name="instance">The object to bind.</param>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze the type of instance so its members may be trimmed")]
	public static void Bind(this IConfiguration configuration, object? instance)
	{
		configuration.Bind(instance, null);
	}

	/// <summary>
	/// Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.
	/// </summary>
	/// <param name="configuration">The configuration instance to bind.</param>
	/// <param name="instance">The object to bind.</param>
	/// <param name="configureOptions">Configures the binder options.</param>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze the type of instance so its members may be trimmed")]
	public static void Bind(this IConfiguration configuration, object? instance, Action<BinderOptions>? configureOptions)
	{
		ArgumentNullException.ThrowIfNull(configuration, "configuration");
		if (instance != null)
		{
			BinderOptions binderOptions = new BinderOptions();
			configureOptions?.Invoke(binderOptions);
			BindingPoint bindingPoint = new BindingPoint(instance, isReadOnly: true);
			BindInstance(instance.GetType(), bindingPoint, configuration, binderOptions, isParentCollection: false);
		}
	}

	/// <summary>
	/// Extracts the value with the specified key and converts it to type T.
	/// </summary>
	/// <typeparam name="T">The type to convert the value to.</typeparam>
	/// <param name="configuration">The configuration.</param>
	/// <param name="key">The key of the configuration section's value to convert.</param>
	/// <returns>The converted value.</returns>
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	public static T? GetValue<T>(this IConfiguration configuration, string key)
	{
		return configuration.GetValue(key, default(T));
	}

	/// <summary>
	/// Extracts the value with the specified key and converts it to type T.
	/// </summary>
	/// <typeparam name="T">The type to convert the value to.</typeparam>
	/// <param name="configuration">The configuration.</param>
	/// <param name="key">The key of the configuration section's value to convert.</param>
	/// <param name="defaultValue">The default value to use if no value is found.</param>
	/// <returns>The converted value.</returns>
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	[return: NotNullIfNotNull("defaultValue")]
	public static T? GetValue<T>(this IConfiguration configuration, string key, T defaultValue)
	{
		return (T)configuration.GetValue(typeof(T), key, defaultValue);
	}

	/// <summary>
	/// Extracts the value with the specified key and converts it to the specified type.
	/// </summary>
	/// <param name="configuration">The configuration.</param>
	/// <param name="type">The type to convert the value to.</param>
	/// <param name="key">The key of the configuration section's value to convert.</param>
	/// <returns>The converted value.</returns>
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	public static object? GetValue(this IConfiguration configuration, Type type, string key)
	{
		return configuration.GetValue(type, key, null);
	}

	/// <summary>
	/// Extracts the value with the specified key and converts it to the specified type.
	/// </summary>
	/// <param name="configuration">The configuration.</param>
	/// <param name="type">The type to convert the value to.</param>
	/// <param name="key">The key of the configuration section's value to convert.</param>
	/// <param name="defaultValue">The default value to use if no value is found.</param>
	/// <returns>The converted value.</returns>
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	[return: NotNullIfNotNull("defaultValue")]
	public static object? GetValue(this IConfiguration configuration, Type type, string key, object? defaultValue)
	{
		ArgumentNullException.ThrowIfNull(configuration, "configuration");
		ArgumentNullException.ThrowIfNull(type, "type");
		IConfigurationSection section = configuration.GetSection(key);
		string value = section.Value;
		if (value != null)
		{
			return ConvertValue(type, value, section.Path);
		}
		return defaultValue;
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze property.PropertyType so its members may be trimmed.")]
	private static void BindProperties(object instance, IConfiguration configuration, BinderOptions options, ParameterInfo[] constructorParameters)
	{
		List<PropertyInfo> allProperties = GetAllProperties(instance.GetType());
		if (options.ErrorOnUnknownConfiguration)
		{
			HashSet<string> hashSet = new HashSet<string>(allProperties.Select((PropertyInfo mp) => mp.Name), StringComparer.OrdinalIgnoreCase);
			List<string> list = null;
			foreach (IConfigurationSection child in configuration.GetChildren())
			{
				if (!hashSet.Contains(child.Key))
				{
					(list ?? (list = new List<string>())).Add("'" + child.Key + "'");
				}
			}
			if (list != null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Error_MissingConfig, "ErrorOnUnknownConfiguration", "BinderOptions", instance.GetType(), string.Join(", ", list)));
			}
		}
		foreach (PropertyInfo property in allProperties)
		{
			if (constructorParameters == null || !constructorParameters.Any((ParameterInfo p) => p.Name == property.Name))
			{
				BindProperty(property, instance, configuration, options);
			}
			else
			{
				ResetPropertyValue(property, instance, options);
			}
		}
	}

	/// <summary>
	/// Reset the property value to the value from the property getter. This is useful for properties that have a getter or setters that perform some logic changing the object state.
	/// </summary>
	/// <param name="property">The property to reset.</param>
	/// <param name="instance">The instance to reset the property on.</param>
	/// <param name="options">The binder options.</param>
	/// <remarks>
	/// This method doesn't do any configuration binding. It just resets the property value to the value from the property getter.
	/// This method called only when creating an instance using a primary constructor with parameters names match properties names.
	/// </remarks>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze property.PropertyType so its members may be trimmed.")]
	private static void ResetPropertyValue(PropertyInfo property, object instance, BinderOptions options)
	{
		if ((object)property.GetMethod != null && (object)property.SetMethod != null && (options.BindNonPublicProperties || (property.GetMethod.IsPublic && property.SetMethod.IsPublic)) && property.GetMethod.GetParameters().Length == 0)
		{
			property.SetValue(instance, property.GetValue(instance));
		}
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze property.PropertyType so its members may be trimmed.")]
	private static void BindProperty(PropertyInfo property, object instance, IConfiguration config, BinderOptions options)
	{
		if (!(property.GetMethod == null) && (options.BindNonPublicProperties || property.GetMethod.IsPublic) && property.GetMethod.GetParameters().Length == 0)
		{
			BindingPoint bindingPoint = new BindingPoint(() => property.GetValue(instance), (object)property.SetMethod == null || (!property.SetMethod.IsPublic && !options.BindNonPublicProperties));
			BindInstance(property.PropertyType, bindingPoint, config.GetSection(GetPropertyName(property)), options, isParentCollection: false);
			if (!bindingPoint.IsReadOnly && (bindingPoint.Value != null || bindingPoint.HasNewValue))
			{
				property.SetValue(instance, bindingPoint.Value);
			}
		}
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	private static void BindInstance(Type type, BindingPoint bindingPoint, IConfiguration config, BinderOptions options, bool isParentCollection)
	{
		if (type == typeof(IConfigurationSection))
		{
			bindingPoint.TrySetValue(config);
		}
		else
		{
			if (config == null)
			{
				return;
			}
			IConfigurationSection configurationSection2;
			bool flag;
			string value;
			if (config is ConfigurationSection configurationSection)
			{
				configurationSection2 = configurationSection;
				flag = configurationSection.TryGetValue(null, out value);
			}
			else
			{
				configurationSection2 = config as IConfigurationSection;
				value = configurationSection2?.Value;
				flag = value != null;
			}
			if (flag && TryConvertValue(type, value, configurationSection2?.Path, out var result, out var error))
			{
				if (error != null)
				{
					throw error;
				}
				if (type == typeof(byte[]) && bindingPoint.Value is byte[] array && array.Length != 0)
				{
					if (result is byte[] array2 && array2.Length != 0)
					{
						Array array3 = Array.CreateInstance(type.GetElementType(), array.Length + array2.Length);
						Array.Copy(array, array3, array.Length);
						Array.Copy(array2, 0, array3, array.Length, array2.Length);
						bindingPoint.TrySetValue(array3);
					}
				}
				else
				{
					bindingPoint.TrySetValue(result);
				}
			}
			else if (config.GetChildren().Any())
			{
				if (type.IsArray || IsImmutableArrayCompatibleInterface(type))
				{
					if (!bindingPoint.IsReadOnly)
					{
						bindingPoint.SetValue(BindArray(type, (IEnumerable)bindingPoint.Value, config, options));
					}
					return;
				}
				if (TypeIsASetInterface(type))
				{
					if (!bindingPoint.IsReadOnly || bindingPoint.Value != null)
					{
						object obj = BindSet(type, (IEnumerable)bindingPoint.Value, config, options);
						if (!bindingPoint.IsReadOnly && obj != null)
						{
							bindingPoint.SetValue(obj);
						}
					}
					return;
				}
				if (TypeIsADictionaryInterface(type))
				{
					if (!bindingPoint.IsReadOnly || bindingPoint.Value != null)
					{
						object obj2 = BindDictionaryInterface(bindingPoint.Value, type, config, options);
						if (!bindingPoint.IsReadOnly && obj2 != null)
						{
							bindingPoint.SetValue(obj2);
						}
					}
					return;
				}
				ParameterInfo[] constructorParameters = null;
				if (bindingPoint.Value == null)
				{
					if (bindingPoint.IsReadOnly)
					{
						return;
					}
					Type type2 = ((type.IsInterface && type.IsConstructedGenericType) ? type.GetGenericTypeDefinition() : null);
					if ((object)type2 != null && (type2 == typeof(ICollection<>) || type2 == typeof(IList<>)))
					{
						Type type3 = typeof(List<>).MakeGenericType(type.GenericTypeArguments);
						bindingPoint.SetValue(Activator.CreateInstance(type3));
					}
					else
					{
						bindingPoint.SetValue(CreateInstance(type, config, options, out constructorParameters));
					}
				}
				Type type4 = FindOpenGenericInterface(typeof(IDictionary<, >), type);
				if (type4 != null)
				{
					BindDictionary(bindingPoint.Value, type4, config, options);
					return;
				}
				Type type5 = FindOpenGenericInterface(typeof(ICollection<>), type);
				if (type5 != null)
				{
					BindCollection(bindingPoint.Value, type5, config, options);
				}
				else
				{
					BindProperties(bindingPoint.Value, config, options, constructorParameters);
				}
			}
			else if (!string.IsNullOrEmpty(value))
			{
				if (options.ErrorOnUnknownConfiguration)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Error_FailedBinding, value, configurationSection2.Path, type));
				}
			}
			else
			{
				if (bindingPoint.IsReadOnly || bindingPoint.Value != null)
				{
					return;
				}
				if (isParentCollection)
				{
					bindingPoint.TrySetValue(CreateInstance(type, config, options, out var _));
				}
				else if (flag)
				{
					if (type.IsArray || IsIEnumerableInterface(type))
					{
						bindingPoint.TrySetValue((value == null) ? null : Array.CreateInstance(type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0], 0));
					}
					else
					{
						bindingPoint.TrySetValue(bindingPoint.Value);
					}
				}
			}
		}
	}

	/// <summary>
	/// Create an instance of the specified type.
	/// </summary>
	/// <param name="type">The type to create an instance of.</param>
	/// <param name="config">The configuration to bind to the instance.</param>
	/// <param name="options">The binder options.</param>
	/// <param name="constructorParameters">The parameters of the constructor used to create the instance.</param>
	/// <returns>The created instance.</returns>
	/// <exception cref="T:System.InvalidOperationException">If the type cannot be created.</exception>
	/// <remarks>
	/// constructorParameters will not be null only when using a constructor with a parameters which get their values from the configuration
	/// This happen when using types having properties match the constructor parameter names. `record` types are an example.
	/// In such cases we need to carry the parameters list to avoid binding the properties again during BindProperties.
	/// </remarks>
	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("In case type is a Nullable<T>, cannot statically analyze what the underlying type is so its members may be trimmed.")]
	private static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, IConfiguration config, BinderOptions options, out ParameterInfo[] constructorParameters)
	{
		constructorParameters = null;
		if (type.IsInterface || type.IsAbstract)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Error_CannotActivateAbstractOrInterface, type));
		}
		ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
		bool flag = type.IsValueType || constructors.Any((ConstructorInfo ctor) => ctor.GetParameters().Length == 0);
		if (!type.IsValueType && constructors.Length == 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Error_MissingPublicInstanceConstructor, type));
		}
		if (constructors.Length > 1 && !flag)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Error_MultipleParameterizedConstructors, type));
		}
		if (constructors.Length == 1 && !flag)
		{
			ConstructorInfo constructorInfo = constructors[0];
			ParameterInfo[] parameters = constructorInfo.GetParameters();
			if (!CanBindToTheseConstructorParameters(parameters, out var nameOfInvalidParameter))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Error_CannotBindToConstructorParameter, type, nameOfInvalidParameter));
			}
			List<PropertyInfo> allProperties = GetAllProperties(type);
			if (!DoAllParametersHaveEquivalentProperties(parameters, allProperties, out var missing))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Error_ConstructorParametersDoNotMatchProperties, type, missing));
			}
			object[] array = new object[parameters.Length];
			for (int num = 0; num < parameters.Length; num++)
			{
				array[num] = BindParameter(parameters[num], type, config, options);
			}
			constructorParameters = parameters;
			return constructorInfo.Invoke(array);
		}
		object obj;
		try
		{
			obj = Activator.CreateInstance(Nullable.GetUnderlyingType(type) ?? type);
		}
		catch (Exception innerException)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Error_FailedToActivate, type), innerException);
		}
		return obj ?? throw new InvalidOperationException(System.SR.Format(System.SR.Error_FailedToActivate, type));
	}

	private static bool DoAllParametersHaveEquivalentProperties(ParameterInfo[] parameters, List<PropertyInfo> properties, out string missing)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PropertyInfo property in properties)
		{
			hashSet.Add(property.Name);
		}
		List<string> list = new List<string>();
		for (int i = 0; i < parameters.Length; i++)
		{
			string name = parameters[i].Name;
			if (!hashSet.Contains(name))
			{
				list.Add(name);
			}
		}
		missing = string.Join(",", list);
		return missing.Length == 0;
	}

	private static bool CanBindToTheseConstructorParameters(ParameterInfo[] constructorParameters, out string nameOfInvalidParameter)
	{
		nameOfInvalidParameter = string.Empty;
		foreach (ParameterInfo parameterInfo in constructorParameters)
		{
			if (parameterInfo.IsOut || parameterInfo.IsIn || parameterInfo.ParameterType.IsByRef)
			{
				nameOfInvalidParameter = parameterInfo.Name;
				return false;
			}
		}
		return true;
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
	private static object BindDictionaryInterface(object source, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type dictionaryType, IConfiguration config, BinderOptions options)
	{
		Type type = dictionaryType.GenericTypeArguments[0];
		Type type2 = dictionaryType.GenericTypeArguments[1];
		bool isEnum = type.IsEnum;
		bool flag = type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
		if (type != typeof(string) && !isEnum && !flag)
		{
			return null;
		}
		MethodInfo method = dictionaryType.GetMethod("Add", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if ((object)method == null || source == null)
		{
			dictionaryType = typeof(Dictionary<, >).MakeGenericType(type, type2);
			object obj = Activator.CreateInstance(dictionaryType);
			method = dictionaryType.GetMethod("Add", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (source is IEnumerable enumerable)
			{
				Type type3 = typeof(KeyValuePair<, >).MakeGenericType(type, type2);
				PropertyInfo property = type3.GetProperty("Key", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				PropertyInfo property2 = type3.GetProperty("Value", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				object[] array = new object[2];
				foreach (object item in enumerable)
				{
					object obj2 = property.GetMethod.Invoke(item, null);
					object obj3 = property2.GetMethod.Invoke(item, null);
					array[0] = obj2;
					array[1] = obj3;
					method.Invoke(obj, array);
				}
			}
			source = obj;
		}
		BindDictionary(source, dictionaryType, config, options);
		return source;
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze what the element type is of the value objects in the dictionary so its members may be trimmed.")]
	private static void BindDictionary(object dictionary, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type dictionaryType, IConfiguration config, BinderOptions options)
	{
		Type type = dictionaryType.GenericTypeArguments[0];
		Type type2 = dictionaryType.GenericTypeArguments[1];
		bool isEnum = type.IsEnum;
		bool flag = type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
		if (type != typeof(string) && !isEnum && !flag)
		{
			return;
		}
		MethodInfo tryGetValue = dictionaryType.GetMethod("TryGetValue", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		PropertyInfo property = dictionaryType.GetProperty("Item", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (IConfigurationSection child in config.GetChildren())
		{
			try
			{
				object key = (isEnum ? Enum.Parse(type, child.Key, ignoreCase: true) : (flag ? Convert.ChangeType(child.Key, type) : child.Key));
				BindingPoint bindingPoint = new BindingPoint(delegate
				{
					object[] array = new object[2] { key, null };
					return (!(bool)tryGetValue.Invoke(dictionary, array)) ? null : array[1];
				}, isReadOnly: false);
				BindInstance(type2, bindingPoint, child, options, isParentCollection: true);
				if (bindingPoint.HasNewValue)
				{
					property.SetValue(dictionary, bindingPoint.Value, new object[1] { key });
				}
			}
			catch (Exception innerException)
			{
				if (options.ErrorOnUnknownConfiguration)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Error_GeneralErrorWhenBinding, "ErrorOnUnknownConfiguration"), innerException);
				}
			}
		}
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze what the element type is of the object collection so its members may be trimmed.")]
	private static void BindCollection(object collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type collectionType, IConfiguration config, BinderOptions options)
	{
		Type type = collectionType.GenericTypeArguments[0];
		MethodInfo method = collectionType.GetMethod("Add", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (IConfigurationSection child in config.GetChildren())
		{
			try
			{
				BindingPoint bindingPoint = new BindingPoint();
				BindInstance(type, bindingPoint, child, options, isParentCollection: true);
				if (bindingPoint.HasNewValue)
				{
					method?.Invoke(collection, new object[1] { bindingPoint.Value });
				}
			}
			catch (Exception innerException)
			{
				if (options.ErrorOnUnknownConfiguration)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Error_GeneralErrorWhenBinding, "ErrorOnUnknownConfiguration"), innerException);
				}
			}
		}
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze what the element type is of the Array so its members may be trimmed.")]
	private static Array BindArray(Type type, IEnumerable source, IConfiguration config, BinderOptions options)
	{
		Type type2;
		if (type.IsArray)
		{
			if (type.GetArrayRank() > 1)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Error_UnsupportedMultidimensionalArray, type));
			}
			type2 = type.GetElementType();
		}
		else
		{
			type2 = type.GetGenericArguments()[0];
		}
		List<object> list = new List<object>();
		if (source != null)
		{
			foreach (object item in source)
			{
				list.Add(item);
			}
		}
		foreach (IConfigurationSection child in config.GetChildren())
		{
			BindingPoint bindingPoint = new BindingPoint();
			try
			{
				BindInstance(type2, bindingPoint, child, options, isParentCollection: true);
				if (bindingPoint.HasNewValue)
				{
					list.Add(bindingPoint.Value);
				}
			}
			catch (Exception innerException)
			{
				if (options.ErrorOnUnknownConfiguration)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Error_GeneralErrorWhenBinding, "ErrorOnUnknownConfiguration"), innerException);
				}
			}
		}
		Array array = Array.CreateInstance(type2, list.Count);
		((ICollection)list).CopyTo(array, 0);
		return array;
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze what the element type is of the Array so its members may be trimmed.")]
	private static object BindSet(Type type, IEnumerable source, IConfiguration config, BinderOptions options)
	{
		Type type2 = type.GetGenericArguments()[0];
		bool isEnum = type2.IsEnum;
		if (type2 != typeof(string) && !isEnum)
		{
			return null;
		}
		object[] array = new object[1];
		MethodInfo method = type.GetMethod("Add", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if ((object)method == null || source == null)
		{
			Type type3 = typeof(HashSet<>).MakeGenericType(type2);
			object obj = Activator.CreateInstance(type3);
			method = type3.GetMethod("Add", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (source != null)
			{
				foreach (object item in source)
				{
					array[0] = item;
					method.Invoke(obj, array);
				}
			}
			source = (IEnumerable)obj;
		}
		foreach (IConfigurationSection child in config.GetChildren())
		{
			BindingPoint bindingPoint = new BindingPoint();
			try
			{
				BindInstance(type2, bindingPoint, child, options, isParentCollection: true);
				if (bindingPoint.HasNewValue)
				{
					array[0] = bindingPoint.Value;
					method.Invoke(source, array);
				}
			}
			catch (Exception innerException)
			{
				if (options.ErrorOnUnknownConfiguration)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Error_GeneralErrorWhenBinding, "ErrorOnUnknownConfiguration"), innerException);
				}
			}
		}
		return source;
	}

	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	private static bool TryConvertValue(Type type, string value, string path, out object result, out Exception error)
	{
		error = null;
		result = null;
		if (type == typeof(object))
		{
			result = value;
			return true;
		}
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			if (string.IsNullOrEmpty(value))
			{
				return true;
			}
			return TryConvertValue(Nullable.GetUnderlyingType(type), value, path, out result, out error);
		}
		TypeConverter converter = TypeDescriptor.GetConverter(type);
		if (converter.CanConvertFrom(typeof(string)))
		{
			try
			{
				if (value != null)
				{
					result = converter.ConvertFromInvariantString(value);
				}
			}
			catch (Exception innerException)
			{
				error = new InvalidOperationException(System.SR.Format(System.SR.Error_FailedBinding, value, path, type), innerException);
			}
			return true;
		}
		if (type == typeof(byte[]))
		{
			try
			{
				if (value != null)
				{
					result = ((value == string.Empty) ? Array.Empty<byte>() : Convert.FromBase64String(value));
				}
			}
			catch (FormatException innerException2)
			{
				error = new InvalidOperationException(System.SR.Format(System.SR.Error_FailedBinding, value, path, type), innerException2);
			}
			return true;
		}
		return false;
	}

	[RequiresUnreferencedCode("In case the type is non-primitive, the trimmer cannot statically analyze the object's type so its members may be trimmed.")]
	private static object ConvertValue(Type type, string value, string path)
	{
		TryConvertValue(type, value, path, out var result, out var error);
		if (error != null)
		{
			throw error;
		}
		return result;
	}

	private static bool TypeIsADictionaryInterface(Type type)
	{
		if (!type.IsInterface || !type.IsConstructedGenericType)
		{
			return false;
		}
		Type genericTypeDefinition = type.GetGenericTypeDefinition();
		if (!(genericTypeDefinition == typeof(IDictionary<, >)))
		{
			return genericTypeDefinition == typeof(IReadOnlyDictionary<, >);
		}
		return true;
	}

	private static bool IsImmutableArrayCompatibleInterface(Type type)
	{
		if (!type.IsInterface || !type.IsConstructedGenericType)
		{
			return false;
		}
		Type genericTypeDefinition = type.GetGenericTypeDefinition();
		if (!(genericTypeDefinition == typeof(IEnumerable<>)) && !(genericTypeDefinition == typeof(IReadOnlyCollection<>)))
		{
			return genericTypeDefinition == typeof(IReadOnlyList<>);
		}
		return true;
	}

	private static bool IsIEnumerableInterface(Type type)
	{
		if (type.IsInterface && type.IsConstructedGenericType)
		{
			return type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
		}
		return false;
	}

	private static bool TypeIsASetInterface(Type type)
	{
		if (!type.IsInterface || !type.IsConstructedGenericType)
		{
			return false;
		}
		Type genericTypeDefinition = type.GetGenericTypeDefinition();
		if (!(genericTypeDefinition == typeof(ISet<>)))
		{
			return genericTypeDefinition == typeof(IReadOnlySet<>);
		}
		return true;
	}

	private static Type FindOpenGenericInterface(Type expected, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type actual)
	{
		if (actual.IsGenericType && actual.GetGenericTypeDefinition() == expected)
		{
			return actual;
		}
		Type[] interfaces = actual.GetInterfaces();
		foreach (Type type in interfaces)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == expected)
			{
				return type;
			}
		}
		return null;
	}

	private static List<PropertyInfo> GetAllProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllProperties)] Type type)
	{
		List<PropertyInfo> list = new List<PropertyInfo>();
		Type type2 = type;
		while (type2 != typeof(object))
		{
			PropertyInfo[] properties = type2.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo in properties)
			{
				MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: true);
				if ((object)setMethod == null || !setMethod.IsVirtual || setMethod == setMethod.GetBaseDefinition())
				{
					list.Add(propertyInfo);
				}
			}
			type2 = type2.BaseType;
		}
		return list;
	}

	[RequiresDynamicCode("Binding strongly typed objects to configuration values requires generating dynamic code at runtime, for example instantiating generic types.")]
	[RequiresUnreferencedCode("Cannot statically analyze property.PropertyType so its members may be trimmed.")]
	private static object BindParameter(ParameterInfo parameter, Type type, IConfiguration config, BinderOptions options)
	{
		string name = parameter.Name;
		if (name == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Error_ParameterBeingBoundToIsUnnamed, type));
		}
		BindingPoint bindingPoint = new BindingPoint(config.GetSection(name).Value);
		BindInstance(parameter.ParameterType, bindingPoint, config.GetSection(name), options, isParentCollection: false);
		if (bindingPoint.Value == null)
		{
			if (!ParameterDefaultValue.TryGetDefaultValue(parameter, out var defaultValue))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Error_ParameterHasNoMatchingConfig, type, name));
			}
			bindingPoint.SetValue(defaultValue);
		}
		return bindingPoint.Value;
	}

	private static string GetPropertyName(PropertyInfo property)
	{
		ArgumentNullException.ThrowIfNull(property, "property");
		foreach (CustomAttributeData customAttributesDatum in property.GetCustomAttributesData())
		{
			if (!(customAttributesDatum.AttributeType != typeof(ConfigurationKeyNameAttribute)))
			{
				if (customAttributesDatum.ConstructorArguments.Count != 1)
				{
					break;
				}
				string text = customAttributesDatum.ConstructorArguments[0].Value?.ToString();
				return (!string.IsNullOrWhiteSpace(text)) ? text : property.Name;
			}
		}
		return property.Name;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
