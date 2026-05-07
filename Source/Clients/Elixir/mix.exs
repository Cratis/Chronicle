# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

defmodule Cratis.Chronicle.Contracts.MixProject do
  use Mix.Project

  @version System.get_env("CHRONICLE_VERSION") || "0.1.0"
  @source_url "https://github.com/Cratis/Chronicle"

  def project do
    [
      app: :cratis_chronicle_contracts,
      version: @version,
      elixir: "~> 1.14",
      start_permanent: Mix.env() == :prod,
      deps: deps(),
      description: "Generated Elixir gRPC contracts for Chronicle with connection handling",
      package: package(),
      source_url: @source_url,
      homepage_url: @source_url
    ]
  end

  def application do
    [
      extra_applications: [:logger]
    ]
  end

  defp deps do
    [
      {:grpc, "~> 0.11"},
      {:mint, "~> 1.7"},
      {:protobuf, "~> 0.16"},
      {:ex_doc, ">= 0.0.0", only: :dev, runtime: false},
      {:protobuf_generate, "~> 0.2", only: :dev, runtime: false}
    ]
  end

  defp package do
    [
      name: "cratis_chronicle_contracts",
      licenses: ["MIT"],
      links: %{"GitHub" => @source_url},
      maintainers: ["Cratis"],
      files: ~w(lib mix.exs README.md .formatter.exs)
    ]
  end
end
