# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule Cratis.Chronicle.Contracts.ChronicleConnectionTest do
  use ExUnit.Case, async: true

  alias Cratis.Chronicle.Contracts.ChronicleConnection

  test "retries the initial connection until it succeeds" do
    responses = [
      {:error, :unavailable},
      {:ok, %{adapter_payload: %{conn_pid: spawn_connection_process()}}}
    ]

    {:ok, attempts} = Agent.start_link(fn -> responses end)

    {:ok, connection} =
      ChronicleConnection.start_link(
        reconnect_base_delay: 0,
        reconnect_max_delay: 0,
        connect_fun: build_connect_fun(attempts),
        disconnect_fun: fn _channel -> :ok end
      )

    assert :ok = ChronicleConnection.connect(connection, 250)
    assert true == ChronicleConnection.connected?(connection)
    assert 0 == Agent.get(attempts, &length/1)
  end

  test "reconnects when the adapter connection process goes down" do
    first_connection_process = spawn_connection_process()
    second_connection_process = spawn_connection_process()

    responses = [
      {:ok, %{name: :first, adapter_payload: %{conn_pid: first_connection_process}}},
      {:ok, %{name: :second, adapter_payload: %{conn_pid: second_connection_process}}}
    ]

    {:ok, attempts} = Agent.start_link(fn -> responses end)

    {:ok, connection} =
      ChronicleConnection.start_link(
        reconnect_base_delay: 0,
        reconnect_max_delay: 0,
        connect_fun: build_connect_fun(attempts),
        disconnect_fun: fn _channel -> :ok end
      )

    assert :ok = ChronicleConnection.connect(connection, 250)
    assert {:ok, %{name: :first}} = ChronicleConnection.channel(connection)

    send(connection, {:elixir_grpc, :connection_down, first_connection_process})

    assert_eventually(fn ->
      match?({:ok, %{name: :second}}, ChronicleConnection.channel(connection))
    end)
  end

  test "disconnect returns not connected afterwards" do
    channel = %{adapter_payload: %{conn_pid: spawn_connection_process()}}
    {:ok, disconnects} = Agent.start_link(fn -> [] end)

    {:ok, connection} =
      ChronicleConnection.start_link(
        reconnect_base_delay: 0,
        reconnect_max_delay: 0,
        connect_fun: fn _target, _options -> {:ok, channel} end,
        disconnect_fun: fn disconnected_channel ->
          Agent.update(disconnects, fn entries -> [disconnected_channel | entries] end)
          :ok
        end
      )

    assert :ok = ChronicleConnection.connect(connection, 250)
    assert :ok = ChronicleConnection.disconnect(connection)
    assert [%{adapter_payload: %{conn_pid: _pid}}] = Agent.get(disconnects, & &1)

    assert catch_exit(ChronicleConnection.connected?(connection))
  end

  defp build_connect_fun(agent) do
    fn _target, _options ->
      Agent.get_and_update(agent, fn
        [response | remaining] -> {response, remaining}
        [] -> {{:error, :no_more_responses}, []}
      end)
    end
  end

  defp spawn_connection_process do
    spawn(fn -> Process.sleep(:infinity) end)
  end

  defp assert_eventually(assertion, attempts \\ 50)

  defp assert_eventually(assertion, attempts) when attempts > 0 do
    if assertion.() do
      :ok
    else
      Process.sleep(10)
      assert_eventually(assertion, attempts - 1)
    end
  end

  defp assert_eventually(_assertion, 0) do
    flunk("Condition was not met in time")
  end
end
