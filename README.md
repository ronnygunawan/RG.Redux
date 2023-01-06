# RG.Redux

[![NuGet](https://img.shields.io/nuget/v/RG.Redux.svg)](https://www.nuget.org/packages/RG.Redux/)

A minimal Redux implementation

### Creating Events, Reducer, and Store

```cs
// Events
public interface IFooEvent : IEvent { }
public record FooIncremented() : IFooEvent;
public record FooDecremented() : IFooEvent;
public record FooIncrementedBy(int Value) : IFooEvent;
public record FooReset() : IFooEvent;

// Store and Reducer
public record FooStore() : Store<int, IFooEvent>(
    reducer: (state, action) =>
        action switch {
            FooIncremented => state + 1,
            FooDecremented => state - 1,
            FooIncrementedBy { Value: var val } => state + val,
            FooReset => 0,
            _ => state
        }.
    initialValue: 0
);

// Creating a Store instance
var fooStore = new FooStore();
```

### Reading State

```cs
var state = fooStore.State;
```

### Dispatching Event

```cs
fooStore.Dispatch(new FooDecremented());
```

### Subscribing to Store updates

```cs
var subscription = fooStore.Subscribe(value => {
    Console.WriteLine(value);
});
```

### Unsubscribing from Store updates

```cs
subscription.Dispose();
```

### Querying Store updates using Reactive.Linq

```cs
var query = from value in fooStore
            where value % 2 == 0
            select value / 2;

var subscription = query.Subscribe(value => {
    Console.WriteLine(value);
});
```

### Don't need event types?

Just mutate the `State` property

```cs
public record FooStore() : Store<int>(0) {
    public void Increment() => State = State + 1;
    public void Decrement() => State = State - 1;
    public void IncrementBy(int x) => State = State + x;
    public void Reset() => State = 0;
}
```
