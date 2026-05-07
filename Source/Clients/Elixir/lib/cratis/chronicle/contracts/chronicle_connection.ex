# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule Cratis.Chronicle.Contracts.ChronicleConnection do
  @moduledoc """
  Manages a resilient Chronicle gRPC channel and reconnects when the transport drops.
  """

  use GenServer

  alias Cratis.Chronicle.Contracts.ChronicleConnectionString

  @default_connect_timeout 10_000
  @default_retry_attempts 5
  @default_reconnect_base_delay 1_000
  @default_reconnect_max_delay 10_000

  @type option ::
          {:connection_string, String.t() | ChronicleConnectionString.t()}
          | {:server_address, String.t()}
          | {:grpc_options, keyword()}
          | {:retry_attempts, non_neg_integer()}
          | {:reconnect_base_delay, non_neg_integer()}
          | {:reconnect_max_delay, non_neg_integer()}
          | {:connect_fun, (String.t(), keyword() -> {:ok, term()} | {:error, term()})}
          | {:disconnect_fun, (term() -> any())}
          | {:name, GenServer.name()}
          | {:auto_connect, boolean()}

  @type state :: %{
          connection_string: ChronicleConnectionString.t(),
          channel: term() | nil,
          connected?: boolean(),
          connect_fun: (String.t(), keyword() -> {:ok, term()} | {:error, term()}),
          disconnect_fun: (term() -> any()),
          grpc_options: keyword(),
          retry_attempts: non_neg_integer(),
          reconnect_base_delay: non_neg_integer(),
          reconnect_max_delay: non_neg_integer(),
          reconnect_attempt: non_neg_integer(),
          reconnect_timer: reference() | nil,
          connection_process: pid() | nil,
          pending_connects: list({GenServer.from(), reference() | nil})
        }

  @doc """
  Starts a Chronicle connection process.
  """
  @spec start_link([option()]) :: GenServer.on_start()
  def start_link(options \\ []) do
    GenServer.start_link(__MODULE__, options, Keyword.take(options, [:name]))
  end

  @doc """
  Waits for the connection to become ready.
  """
  @spec connect(GenServer.server(), timeout()) :: :ok | {:error, :timeout}
  def connect(connection, timeout \\ @default_connect_timeout) do
    GenServer.call(connection, {:await_connected, timeout}, call_timeout(timeout))
  end

  @doc """
  Gets whether the channel is currently connected.
  """
  @spec connected?(GenServer.server()) :: boolean()
  def connected?(connection) do
    GenServer.call(connection, :connected?)
  end

  @doc """
  Gets the current channel when connected.
  """
  @spec channel(GenServer.server()) :: {:ok, term()} | {:error, :not_connected}
  def channel(connection) do
    GenServer.call(connection, :channel)
  end

  @doc """
  Disconnects the active channel and stops reconnect attempts.
  """
  @spec disconnect(GenServer.server()) :: :ok
  def disconnect(connection) do
    GenServer.call(connection, :disconnect)
  end

  @impl true
  def init(options) do
    state = %{
      connection_string: connection_string_from(options),
      channel: nil,
      connected?: false,
      connect_fun: Keyword.get(options, :connect_fun, &default_connect/2),
      disconnect_fun: Keyword.get(options, :disconnect_fun, &default_disconnect/1),
      grpc_options: Keyword.get(options, :grpc_options, []),
      retry_attempts: Keyword.get(options, :retry_attempts, @default_retry_attempts),
      reconnect_base_delay: Keyword.get(options, :reconnect_base_delay, @default_reconnect_base_delay),
      reconnect_max_delay: Keyword.get(options, :reconnect_max_delay, @default_reconnect_max_delay),
      reconnect_attempt: 0,
      reconnect_timer: nil,
      connection_process: nil,
      pending_connects: []
    }

    if Keyword.get(options, :auto_connect, true) do
      send(self(), :connect)
    end

    {:ok, state}
  end

  @impl true
  def handle_call({:await_connected, _timeout}, _from, %{connected?: true} = state) do
    {:reply, :ok, state}
  end

  def handle_call({:await_connected, timeout}, from, state) do
    timer_ref =
      if timeout == :infinity do
        nil
      else
        Process.send_after(self(), {:connect_timeout, from}, timeout)
      end

    {:noreply, %{state | pending_connects: [{from, timer_ref} | state.pending_connects]}}
  end

  def handle_call(:connected?, _from, state) do
    {:reply, state.connected?, state}
  end

  def handle_call(:channel, _from, %{channel: channel, connected?: true} = state) do
    {:reply, {:ok, channel}, state}
  end

  def handle_call(:channel, _from, state) do
    {:reply, {:error, :not_connected}, state}
  end

  def handle_call(:disconnect, _from, state) do
    disconnect_channel(state)
    state = fail_pending_connects(state, {:error, :disconnected})
    {:stop, :normal, :ok, %{state | connected?: false, channel: nil, connection_process: nil, reconnect_timer: nil}}
  end

  @impl true
  def handle_info(:connect, %{connected?: true} = state) do
    {:noreply, state}
  end

  def handle_info(:connect, state) do
    {:noreply, attempt_connect(%{state | reconnect_timer: nil})}
  end

  def handle_info({:connect_timeout, from}, state) do
    {matches, remaining} =
      Enum.split_with(state.pending_connects, fn {pending_from, _timer_ref} -> pending_from == from end)

    Enum.each(matches, fn {pending_from, _timer_ref} ->
      GenServer.reply(pending_from, {:error, :timeout})
    end)

    {:noreply, %{state | pending_connects: remaining}}
  end

  def handle_info({:elixir_grpc, :connection_down, pid}, state) when pid == state.connection_process do
    {:noreply, handle_connection_down(state)}
  end

  def handle_info({:gun_down, pid, _protocol, _reason}, state) when pid == state.connection_process do
    {:noreply, handle_connection_down(state)}
  end

  def handle_info({:gun_down, pid, _protocol, _reason, _streams}, state) when pid == state.connection_process do
    {:noreply, handle_connection_down(state)}
  end

  def handle_info(_message, state) do
    {:noreply, state}
  end

  defp attempt_connect(state) do
    target = target_for(state.connection_string)

    case state.connect_fun.(target, grpc_options_for(state)) do
      {:ok, channel} ->
        succeed_connect(state, channel)

      {:error, _reason} ->
        schedule_reconnect(%{state | channel: nil, connected?: false, connection_process: nil})
    end
  end

  defp succeed_connect(state, channel) do
    connection_process = connection_process_for(channel)

    state
    |> disconnect_channel()
    |> Map.merge(%{
      channel: channel,
      connected?: true,
      reconnect_attempt: 0,
      reconnect_timer: nil,
      connection_process: connection_process
    })
    |> reply_pending_connects(:ok)
  end

  defp handle_connection_down(state) do
    state
    |> disconnect_channel()
    |> Map.merge(%{
      connected?: false,
      channel: nil,
      connection_process: nil
    })
    |> schedule_reconnect()
  end

  defp schedule_reconnect(%{reconnect_timer: timer_ref} = state) when not is_nil(timer_ref), do: state

  defp schedule_reconnect(state) do
    delay =
      state.reconnect_base_delay
      |> Kernel.*(Integer.pow(2, state.reconnect_attempt))
      |> min(state.reconnect_max_delay)

    timer_ref = Process.send_after(self(), :connect, delay)

    %{state | reconnect_timer: timer_ref, reconnect_attempt: state.reconnect_attempt + 1}
  end

  defp reply_pending_connects(state, reply) do
    Enum.each(state.pending_connects, fn {from, timer_ref} ->
      cancel_timer(timer_ref)
      GenServer.reply(from, reply)
    end)

    %{state | pending_connects: []}
  end

  defp fail_pending_connects(state, reply) do
    reply_pending_connects(state, reply)
  end

  defp disconnect_channel(%{channel: nil} = state), do: state

  defp disconnect_channel(%{channel: channel, disconnect_fun: disconnect_fun} = state) do
    cancel_timer(state.reconnect_timer)

    try do
      disconnect_fun.(channel)
    rescue
      _error -> :ok
    end

    %{state | reconnect_timer: nil}
  end

  defp connection_string_from(options) do
    cond do
      match?(%ChronicleConnectionString{}, options[:connection_string]) ->
        options[:connection_string]

      is_binary(options[:connection_string]) ->
        ChronicleConnectionString.parse(options[:connection_string])

      is_binary(options[:server_address]) ->
        ChronicleConnectionString.parse("chronicle://#{options[:server_address]}")

      true ->
        ChronicleConnectionString.default()
    end
  end

  defp target_for(connection_string) do
    "#{connection_string.server_address.host}:#{connection_string.server_address.port}"
  end

  defp grpc_options_for(state) do
    headers =
      case state.connection_string.api_key do
        nil -> []
        api_key -> [{"api-key", api_key}]
      end

    options =
      [
        adapter: GRPC.Client.Adapters.Mint,
        retry: state.retry_attempts,
        headers: headers
      ]
      |> Keyword.merge(state.grpc_options)

    if state.connection_string.disable_tls or not Code.ensure_loaded?(GRPC.Credential) do
      options
    else
      credential = apply(GRPC.Credential, :new, [[ssl: []]])
      Keyword.put_new(options, :cred, credential)
    end
  end

  defp connection_process_for(%{adapter_payload: %{conn_pid: pid}}) when is_pid(pid), do: pid
  defp connection_process_for(_channel), do: nil

  defp cancel_timer(nil), do: :ok
  defp cancel_timer(timer_ref), do: Process.cancel_timer(timer_ref)

  defp call_timeout(:infinity), do: :infinity
  defp call_timeout(timeout) when is_integer(timeout), do: timeout + 100

  defp default_connect(target, options), do: apply(GRPC.Stub, :connect, [target, options])
  defp default_disconnect(channel), do: apply(GRPC.Stub, :disconnect, [channel])
end
