# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule Cratis.Chronicle.Contracts.ChronicleConnectionString do
  @moduledoc """
  Parses and formats Chronicle connection strings.
  """

  @default_port 35_000
  @development_client "chronicle-dev-client"
  @development_client_secret "chronicle-dev-secret"

  defmodule ServerAddress do
    @moduledoc """
    Represents a Chronicle server address.
    """

    defstruct host: nil, port: nil

    @type t :: %__MODULE__{
            host: String.t() | nil,
            port: non_neg_integer() | nil
          }
  end

  defstruct scheme: "chronicle",
            server_address: nil,
            username: nil,
            password: nil,
            api_key: nil,
            disable_tls: false,
            certificate_path: nil,
            certificate_password: nil,
            query_parameters: %{}

  @type t :: %__MODULE__{
          scheme: String.t(),
          server_address: ServerAddress.t() | nil,
          username: String.t() | nil,
          password: String.t() | nil,
          api_key: String.t() | nil,
          disable_tls: boolean(),
          certificate_path: String.t() | nil,
          certificate_password: String.t() | nil,
          query_parameters: %{optional(String.t()) => String.t()}
        }

  @doc """
  Gets the default local development connection string without authentication.
  """
  @spec default() :: t()
  def default do
    parse("chronicle://localhost:#{@default_port}")
  end

  @doc """
  Gets the Chronicle development connection string with default credentials.
  """
  @spec development() :: t()
  def development do
    parse("chronicle://#{@development_client}:#{@development_client_secret}@localhost:#{@default_port}")
  end

  @doc """
  Parses a Chronicle connection string into a struct.
  """
  @spec parse(String.t()) :: t()
  def parse(connection_string) when is_binary(connection_string) do
    uri = URI.parse(connection_string)
    scheme = uri.scheme || raise ArgumentError, "Connection string must include a scheme"

    if scheme not in ["chronicle", "chronicle+srv"] do
      raise ArgumentError, "Unsupported Chronicle scheme '#{scheme}'"
    end

    host = uri.host || raise ArgumentError, "Connection string must include a host"
    port = if uri.port in [nil, -1], do: @default_port, else: uri.port

    if port < 1 or port > 65_535 do
      raise ArgumentError, "Connection string port must be between 1 and 65535"
    end

    {username, password} = parse_user_info(uri.userinfo)
    query_parameters = parse_query(uri.query)

    %__MODULE__{
      scheme: scheme,
      server_address: %ServerAddress{host: host, port: port},
      username: username,
      password: password,
      api_key: Map.get(query_parameters, "apiKey"),
      disable_tls: String.downcase(Map.get(query_parameters, "disableTls", "false")) == "true",
      certificate_path: Map.get(query_parameters, "certificatePath"),
      certificate_password: Map.get(query_parameters, "certificatePassword"),
      query_parameters: query_parameters
    }
  end

  @doc """
  Gets the configured authentication mode.
  """
  @spec authentication_mode(t()) :: :client_credentials | :api_key
  def authentication_mode(%__MODULE__{} = connection_string) do
    has_credentials? =
      present?(connection_string.username) and
        present?(connection_string.password)

    has_api_key? = present?(connection_string.api_key)

    cond do
      has_credentials? and has_api_key? ->
        raise ArgumentError, "Cannot specify both client credentials and API key authentication"

      has_credentials? ->
        :client_credentials

      has_api_key? ->
        :api_key

      true ->
        raise ArgumentError, "No authentication method specified"
    end
  end

  @doc """
  Creates a new connection string with client credentials.
  """
  @spec with_credentials(t(), String.t(), String.t()) :: t()
  def with_credentials(%__MODULE__{} = connection_string, username, password) do
    updated_query_parameters = Map.delete(connection_string.query_parameters, "apiKey")

    %{connection_string | username: username, password: password, api_key: nil, query_parameters: updated_query_parameters}
  end

  @doc """
  Creates a new connection string with an API key.
  """
  @spec with_api_key(t(), String.t()) :: t()
  def with_api_key(%__MODULE__{} = connection_string, api_key) do
    query_parameters = Map.put(connection_string.query_parameters, "apiKey", api_key)

    %{connection_string | username: nil, password: nil, api_key: api_key, query_parameters: query_parameters}
  end

  @doc """
  Converts a parsed connection string back into its URI representation.
  """
  @spec format(t()) :: String.t()
  def format(%__MODULE__{} = connection_string) do
    authority =
      build_authority(
        connection_string.server_address,
        connection_string.username,
        connection_string.password
      )

    query =
      connection_string.query_parameters
      |> Enum.sort_by(fn {key, _value} -> key end)
      |> Enum.map_join("&", fn {key, value} ->
        "#{URI.encode_www_form(key)}=#{URI.encode_www_form(value)}"
      end)

    if query == "" do
      "#{connection_string.scheme}://#{authority}"
    else
      "#{connection_string.scheme}://#{authority}?#{query}"
    end
  end

  defp parse_user_info(nil), do: {nil, nil}

  defp parse_user_info(user_info) do
    case String.split(user_info, ":", parts: 2) do
      [username, password] ->
        {URI.decode_www_form(username), URI.decode_www_form(password)}

      [username] ->
        {URI.decode_www_form(username), nil}
    end
  end

  defp parse_query(nil), do: %{}

  defp parse_query(query) do
    URI.decode_query(query)
  end

  defp build_authority(server_address, username, password) do
    credentials =
      cond do
        present?(username) and present?(password) ->
          "#{URI.encode_www_form(username)}:#{URI.encode_www_form(password)}@"

        present?(username) ->
          "#{URI.encode_www_form(username)}@"

        true ->
          ""
      end

    "#{credentials}#{server_address.host}:#{server_address.port}"
  end

  defp present?(value), do: is_binary(value) and value != ""
end

defimpl String.Chars, for: Cratis.Chronicle.Contracts.ChronicleConnectionString do
  # Copyright (c) Cratis. All rights reserved.
  # Licensed under the MIT license. See LICENSE file in the project root for full license information.

  def to_string(connection_string) do
    Cratis.Chronicle.Contracts.ChronicleConnectionString.format(connection_string)
  end
end
