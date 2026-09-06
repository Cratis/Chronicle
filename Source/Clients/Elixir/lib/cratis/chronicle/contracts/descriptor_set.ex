# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule Cratis.Chronicle.Contracts.DescriptorSet do
  @moduledoc """
  The canonical descriptor set describing every contract in this version of Chronicle.

  A client hands this to the kernel on connect and the kernel answers whether it still serves them. It ships as a
  build artifact rather than being rebuilt at runtime because the same set has to be byte-identical across all four
  Chronicle client SDKs, and `protoc-gen-elixir` keeps no descriptors at runtime.
  """

  @descriptor_path Path.join([__DIR__, "..", "..", "..", "..", "priv", "protos", "chronicle.desc"])
  @external_resource @descriptor_path

  # Read at compile time so the bytes travel inside the built module rather than depending on priv/ being laid out
  # the same way wherever the package ends up.
  @descriptor_set (case File.read(@descriptor_path) do
                     {:ok, contents} -> contents
                     {:error, _} -> <<>>
                   end)

  @doc """
  The serialized `FileDescriptorSet` for this version of the contracts.
  """
  @spec bytes() :: binary()
  def bytes, do: @descriptor_set

  @doc """
  The version of the contracts this package carries - the protocol version it speaks.
  """
  @spec protocol_version() :: String.t()
  def protocol_version do
    case :application.get_key(:cratis_chronicle_contracts, :vsn) do
      {:ok, version} -> List.to_string(version)
      _ -> "0.0.0"
    end
  end
end
