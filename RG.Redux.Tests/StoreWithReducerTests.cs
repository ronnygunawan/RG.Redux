using RG.Redux;
using Shouldly;
using System.Reactive.Linq;

namespace RG.Redux.Tests;

/// <summary>
/// Tests for Store<TState, TEvent> with reducer and events
/// </summary>
public class StoreWithReducerTests {
	// Test Events
	public interface ICounterEvent : IEvent { }
	public record Incremented() : ICounterEvent;
	public record Decremented() : ICounterEvent;
	public record IncrementedBy(int Value) : ICounterEvent;
	public record Reset() : ICounterEvent;
	public record UnknownEvent() : ICounterEvent;

	private static Store<int, ICounterEvent> CreateCounterStore(int initialState = 0) {
		return new Store<int, ICounterEvent>(
			reducer: (state, action) =>
				action switch {
					Incremented => state + 1,
					Decremented => state - 1,
					IncrementedBy { Value: var val } => state + val,
					Reset => 0,
					_ => state
				},
			initialState: initialState
		);
	}

	[Fact]
	public void Constructor_ShouldSetInitialState() {
		// Arrange & Act
		using var store = CreateCounterStore(5);

		// Assert
		store.State.ShouldBe(5);
	}

	[Fact]
	public void Dispatch_WithIncremented_ShouldIncrementState() {
		// Arrange
		using var store = CreateCounterStore(0);

		// Act
		store.Dispatch(new Incremented());

		// Assert
		store.State.ShouldBe(1);
	}

	[Fact]
	public void Dispatch_WithDecremented_ShouldDecrementState() {
		// Arrange
		using var store = CreateCounterStore(5);

		// Act
		store.Dispatch(new Decremented());

		// Assert
		store.State.ShouldBe(4);
	}

	[Fact]
	public void Dispatch_WithIncrementedBy_ShouldIncrementByValue() {
		// Arrange
		using var store = CreateCounterStore(10);

		// Act
		store.Dispatch(new IncrementedBy(5));

		// Assert
		store.State.ShouldBe(15);
	}

	[Fact]
	public void Dispatch_WithReset_ShouldResetToZero() {
		// Arrange
		using var store = CreateCounterStore(100);

		// Act
		store.Dispatch(new Reset());

		// Assert
		store.State.ShouldBe(0);
	}

	[Fact]
	public void Dispatch_WithUnknownEvent_ShouldNotChangeState() {
		// Arrange
		using var store = CreateCounterStore(42);

		// Act
		store.Dispatch(new UnknownEvent());

		// Assert
		store.State.ShouldBe(42);
	}

	[Fact]
	public void Dispatch_MultipleEvents_ShouldApplySequentially() {
		// Arrange
		using var store = CreateCounterStore(0);

		// Act
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());
		store.Dispatch(new IncrementedBy(5));
		store.Dispatch(new Decremented());

		// Assert
		store.State.ShouldBe(6); // 0 + 1 + 1 + 5 - 1 = 6
	}

	[Fact]
	public void Dispatch_ShouldReturnDispatchedEvent() {
		// Arrange
		using var store = CreateCounterStore(0);
		var @event = new Incremented();

		// Act
		var result = store.Dispatch(@event);

		// Assert
		result.ShouldBe(@event);
	}

	[Fact]
	public void Subscribe_ShouldReceiveInitialState() {
		// Arrange
		using var store = CreateCounterStore(5);
		int? receivedValue = null;

		// Act
		using var subscription = store.Subscribe(value => receivedValue = value);

		// Assert
		receivedValue.ShouldBe(5);
	}

	[Fact]
	public void Subscribe_ShouldReceiveStateChanges() {
		// Arrange
		using var store = CreateCounterStore(0);
		var receivedValues = new List<int>();

		// Act
		using var subscription = store.Subscribe(value => receivedValues.Add(value));
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());

		// Assert
		receivedValues.ShouldBe(new[] { 0, 1, 2 });
	}

	[Fact]
	public void Subscribe_MultipleSubscribers_ShouldAllReceiveUpdates() {
		// Arrange
		using var store = CreateCounterStore(0);
		var receivedValues1 = new List<int>();
		var receivedValues2 = new List<int>();

		// Act
		using var subscription1 = store.Subscribe(value => receivedValues1.Add(value));
		using var subscription2 = store.Subscribe(value => receivedValues2.Add(value));
		store.Dispatch(new Incremented());

		// Assert
		receivedValues1.ShouldBe(new[] { 0, 1 });
		receivedValues2.ShouldBe(new[] { 0, 1 });
	}

	[Fact]
	public void Unsubscribe_ShouldStopReceivingUpdates() {
		// Arrange
		using var store = CreateCounterStore(0);
		var receivedValues = new List<int>();
		var subscription = store.Subscribe(value => receivedValues.Add(value));

		// Act
		subscription.Dispose();
		store.Dispatch(new Incremented());

		// Assert
		receivedValues.ShouldBe(new[] { 0 }); // Only initial state
	}

	[Fact]
	public void Subscribe_AfterDisposal_ShouldNotThrow() {
		// Arrange
		using var store = CreateCounterStore(0);
		var receivedValues = new List<int>();

		// Act
		using (var subscription = store.Subscribe(value => receivedValues.Add(value))) {
			store.Dispatch(new Incremented());
		}
		store.Dispatch(new Incremented());

		// Assert
		receivedValues.ShouldBe(new[] { 0, 1 }); // Only values before disposal
	}

	[Fact]
	public void Dispose_ShouldDisposeUnderlyingSubject() {
		// Arrange
		var store = CreateCounterStore(0);

		// Act
		store.Dispose();

		// Assert
		Should.Throw<ObjectDisposedException>(() => store.Subscribe(_ => { }));
	}

	[Fact]
	public void Dispose_Multiple_ShouldNotThrow() {
		// Arrange
		using var store = CreateCounterStore(0);

		// Act & Assert
		Should.NotThrow(() => {
			store.Dispose();
			store.Dispose();
		});
	}

	[Fact]
	public void Store_AsObservable_ShouldWorkWithLinq() {
		// Arrange
		using var store = CreateCounterStore(0);
		var evenValues = new List<int>();
		var query = store.Where(value => value % 2 == 0);

		// Act
		using var subscription = query.Subscribe(value => evenValues.Add(value));
		store.Dispatch(new Incremented()); // 1 - odd
		store.Dispatch(new Incremented()); // 2 - even
		store.Dispatch(new Incremented()); // 3 - odd
		store.Dispatch(new Incremented()); // 4 - even

		// Assert
		evenValues.ShouldBe(new[] { 0, 2, 4 });
	}

	[Fact]
	public void Store_AsObservable_ShouldWorkWithSelect() {
		// Arrange
		using var store = CreateCounterStore(0);
		var doubledValues = new List<int>();
		var query = store.Select(value => value * 2);

		// Act
		using var subscription = query.Subscribe(value => doubledValues.Add(value));
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());

		// Assert
		doubledValues.ShouldBe(new[] { 0, 2, 4 });
	}

	[Fact]
	public void Store_WithComplexState_ShouldWork() {
		// Arrange
		using var store = new Store<string, ICounterEvent>(
			reducer: (state, action) =>
				action switch {
					Incremented => state + "1",
					Decremented => state.Length > 0 ? state[..^1] : state,
					Reset => "",
					_ => state
				},
			initialState: "hello"
		);

		// Act
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());

		// Assert
		store.State.ShouldBe("hello11");
	}
}
