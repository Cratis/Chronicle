# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule Cratis.Chronicle.Contracts.ChronicleConnectionStringTest do
  use ExUnit.Case, async: true

  alias Cratis.Chronicle.Contracts.ChronicleConnectionString

  test "parses api key connection strings" do
    connection_string =
      ChronicleConnectionString.new(
        "chronicle://localhost:35000?apiKey=secret&disableTls=true"
      )

    assert connection_string.server_address.host == "localhost"
    assert connection_string.server_address.port == 35_000
    assert connection_string.api_key == "secret"
    assert connection_string.disable_tls
    assert ChronicleConnectionString.authentication_mode(connection_string) == :api_key
  end

  test "formats credentials when converted back to a string" do
    connection_string =
      ChronicleConnectionString.default()
      |> ChronicleConnectionString.with_credentials("client", "secret")

    assert ChronicleConnectionString.to_string(connection_string) ==
             "chronicle://client:secret@localhost:35000"
  end
end
