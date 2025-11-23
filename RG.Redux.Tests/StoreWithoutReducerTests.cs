using RG.Redux;
using Shouldly;

namespace RG.Redux.Tests;

/// <summary>
/// Tests for Store<TState> without event types (direct state mutation)
/// </summary>
public class StoreWithoutReducerTests {
	// Custom store that extends Store<int> for direct state mutation
	public record CounterStore() : Store<int>(initialState: 0) {
		public void Increment() => State++;
		public void Decrement() => State--;
		public void IncrementBy(int x) => State += x;
		public void Reset() => State = 0;
		public void SetValue(int value) => State = value;
	}

	[Fact]
	public void Constructor_ShouldSetInitialState() {
		// Arrange & Act
		using var store = new CounterStore();

		// Assert
		store.State.ShouldBe(0);
	}

	[Fact]
	public void Constructor_WithCustomInitialState_ShouldSetState() {
		// Arrange
		var initialState = 42;

		// Act
		using var store = new Store<int>(initialState);

		// Assert
		store.State.ShouldBe(42);
	}

	[Fact]
	public void Increment_ShouldIncrementState() {
		// Arrange
		using var store = new CounterStore();

		// Act
		store.Increment();

		// Assert
		store.State.ShouldBe(1);
	}

	[Fact]
	public void Decrement_ShouldDecrementState() {
		// Arrange
		using var store = new CounterStore();
		store.SetValue(5);

		// Act
		store.Decrement();

		// Assert
		store.State.ShouldBe(4);
	}

	[Fact]
	public void IncrementBy_ShouldIncrementByValue() {
		// Arrange
		using var store = new CounterStore();
		store.SetValue(10);

		// Act
		store.IncrementBy(7);

		// Assert
		store.State.ShouldBe(17);
	}

	[Fact]
	public void Reset_ShouldResetToZero() {
		// Arrange
		using var store = new CounterStore();
		store.SetValue(100);

		// Act
		store.Reset();

		// Assert
		store.State.ShouldBe(0);
	}

	[Fact]
	public void SetValue_ShouldSetState() {
		// Arrange
		using var store = new CounterStore();

		// Act
		store.SetValue(42);

		// Assert
		store.State.ShouldBe(42);
	}

	[Fact]
	public void MultipleMutations_ShouldApplySequentially() {
		// Arrange
		using var store = new CounterStore();

		// Act
		store.Increment();
		store.Increment();
		store.IncrementBy(5);
		store.Decrement();

		// Assert
		store.State.ShouldBe(6); // 0 + 1 + 1 + 5 - 1 = 6
	}

	[Fact]
	public void Subscribe_ShouldReceiveInitialState() {
		// Arrange
		using var store = new CounterStore();
		int? receivedValue = null;

		// Act
		using var subscription = store.Subscribe(value => receivedValue = value);

		// Assert
		receivedValue.ShouldBe(0);
	}

	[Fact]
	public void Subscribe_ShouldReceiveStateChanges() {
		// Arrange
		using var store = new CounterStore();
		var receivedValues = new List<int>();

		// Act
		using var subscription = store.Subscribe(value => receivedValues.Add(value));
		store.Increment();
		store.Increment();

		// Assert
		receivedValues.ShouldBe(new[] { 0, 1, 2 });
	}

	[Fact]
	public void Subscribe_WithSetValue_ShouldReceiveChanges() {
		// Arrange
		using var store = new CounterStore();
		var receivedValues = new List<int>();

		// Act
		using var subscription = store.Subscribe(value => receivedValues.Add(value));
		store.SetValue(10);
		store.SetValue(20);

		// Assert
		receivedValues.ShouldBe(new[] { 0, 10, 20 });
	}

	[Fact]
	public void Dispatch_WithoutReducer_ShouldThrowInvalidOperationException() {
		// Arrange
		using var store = new Store<int>(initialState: 0);

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => store.Dispatch(new TestEvent()));
	}

	[Fact]
	public void Dispose_ShouldDisposeUnderlyingSubject() {
		// Arrange
		var store = new CounterStore();

		// Act
		store.Dispose();

		// Assert
		Should.Throw<ObjectDisposedException>(() => store.Subscribe(_ => { }));
	}

	[Fact]
	public void Dispose_Multiple_ShouldNotThrow() {
		// Arrange
		using var store = new CounterStore();

		// Act & Assert
		Should.NotThrow(() => {
			store.Dispose();
			store.Dispose();
		});
	}

	[Fact]
	public void Store_WithComplexState_ShouldWork() {
		// Arrange
		using var store = new StringStore();

		// Act
		store.Append("Hello");
		store.Append(" ");
		store.Append("World");

		// Assert
		store.State.ShouldBe("Hello World");
	}

	[Fact]
	public void Store_WithRecordState_ShouldWork() {
		// Arrange
		using var store = new PersonStore();

		// Act
		store.UpdateName("Alice");
		store.UpdateAge(30);

		// Assert
		store.State.Name.ShouldBe("Alice");
		store.State.Age.ShouldBe(30);
	}

	// Helper classes for complex state tests
	private record TestEvent() : IEvent;

	private record StringStore() : Store<string>(initialState: "") {
		public void Append(string text) => State += text;
		public void Clear() => State = "";
	}

	private record Person(string Name, int Age);

	private record PersonStore() : Store<Person>(initialState: new Person("", 0)) {
		public void UpdateName(string name) => State = State with { Name = name };
		public void UpdateAge(int age) => State = State with { Age = age };
	}
}
