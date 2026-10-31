<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/reactor-example.md -->
# Reactor and read model examples

This reference holds a worked reactor module and a worked model-bound read model.

```elixir
defmodule MyApp.Reactors.DepositLogger do
  use Chronicle.Reactors.Reactor
  require Logger

  alias MyApp.Events.FundsDeposited
  @handles FundsDeposited

  @impl true
  def handle(%FundsDeposited{}, %{sequence_number: number}) do
    Logger.info("Deposit observed", sequence_number: number)
    :ok
  end
end
```

## Read model example

```elixir
defmodule MyApp.ReadModels.AccountBalance do
  use Chronicle.ReadModels.ReadModel
  defstruct id: "", balance: 0

  from(MyApp.Events.FundsDeposited,
    set: [id: :event_source_id],
    add: [balance: :amount]
  )
end
```
