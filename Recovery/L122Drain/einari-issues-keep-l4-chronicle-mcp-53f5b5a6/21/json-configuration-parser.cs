using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration.Json;

internal sealed class JsonConfigurationFileParser
{
	private readonly Dictionary<string, string> _data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly Stack<string> _paths = new Stack<string>();

	private JsonConfigurationFileParser()
	{
	}

	public static IDictionary<string, string?> Parse(Stream input)
	{
		return new JsonConfigurationFileParser().ParseStream(input);
	}

	private Dictionary<string, string> ParseStream(Stream input)
	{
		JsonDocumentOptions options = new JsonDocumentOptions
		{
			CommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true
		};
		using (StreamReader streamReader = new StreamReader(input))
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(streamReader.ReadToEnd(), options);
			if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
			{
				throw new FormatException(System.SR.Format(System.SR.Error_InvalidTopLevelJSONElement, jsonDocument.RootElement.ValueKind));
			}
			VisitObjectElement(jsonDocument.RootElement);
		}
		return _data;
	}

	private void VisitObjectElement(JsonElement element)
	{
		bool nullIfElementIsEmpty = true;
		foreach (JsonProperty item in element.EnumerateObject())
		{
			nullIfElementIsEmpty = false;
			EnterContext(item.Name);
			VisitValue(item.Value);
			ExitContext();
		}
		SetNullIfElementIsEmpty(nullIfElementIsEmpty);
	}

	private void VisitArrayElement(JsonElement element)
	{
		int num = 0;
		foreach (JsonElement item in element.EnumerateArray())
		{
			EnterContext(num.ToString());
			VisitValue(item);
			ExitContext();
			num++;
		}
		SetEmptyIfElementIsEmpty(num == 0);
	}

	private void SetNullIfElementIsEmpty(bool isEmpty)
	{
		if (isEmpty && _paths.Count > 0)
		{
			_data[_paths.Peek()] = null;
		}
	}

	private void SetEmptyIfElementIsEmpty(bool isEmpty)
	{
		if (isEmpty && _paths.Count > 0)
		{
			_data[_paths.Peek()] = string.Empty;
		}
	}

	private void VisitValue(JsonElement value)
	{
		switch (value.ValueKind)
		{
		case JsonValueKind.Object:
			VisitObjectElement(value);
			break;
		case JsonValueKind.Array:
			VisitArrayElement(value);
			break;
		case JsonValueKind.String:
		case JsonValueKind.Number:
		case JsonValueKind.True:
		case JsonValueKind.False:
		case JsonValueKind.Null:
		{
			string text = _paths.Peek();
			if (_data.ContainsKey(text))
			{
				throw new FormatException(System.SR.Format(System.SR.Error_KeyIsDuplicated, text));
			}
			_data[text] = ((value.ValueKind == JsonValueKind.Null) ? null : value.ToString());
			break;
		}
		default:
			throw new FormatException(System.SR.Format(System.SR.Error_UnsupportedJSONToken, value.ValueKind));
		}
	}

	private void EnterContext(string context)
	{
		_paths.Push((_paths.Count > 0) ? (_paths.Peek() + ConfigurationPath.KeyDelimiter + context) : context);
	}

	private void ExitContext()
	{
		_paths.Pop();
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
