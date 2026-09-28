# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule Cratis.Chronicle.Contracts.VersionTest do
  use ExUnit.Case, async: true

  test "the package reports the version stored in its published files" do
    version = "VERSION" |> File.read!() |> String.trim()
    project = Mix.Project.config()

    assert project[:version] == version
    assert "VERSION" in project[:package][:files]
  end
end
