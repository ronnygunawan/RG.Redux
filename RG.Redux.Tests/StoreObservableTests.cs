using RG.Redux;
using Shouldly;
using System.Reactive.Linq;

namespace RG.Redux.Tests;

/// <summary>
/// Tests for observable/reactive features of the Store
/// </summary>
public class StoreObservableTests {
	public interface ICounterEvent : IEvent { }
	public record Incremented() : ICounterEvent;
	public record Decremented() : ICounterEvent;
	public record SetValue(int Value) : ICounterEvent;

	private static Store<int, ICounterEvent> CreateCounterStore(int initialState = 0) {
		return new Store<int, ICounterEvent>(
			reducer: (state, action) =>
				action switch {
					Incremented => state + 1,
					Decremented => state - 1,
					SetValue { Value: var val } => val,
					_ => state
				},
			initialState: initialState
		);
	}

	[Fact]
	public void Where_ShouldFilterValues() {
		// Arrange
		using var store = CreateCounterStore(0);
		var evenValues = new List<int>();
		var query = from value in store
					where value % 2 == 0
					select value;

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
	public void Select_ShouldTransformValues() {
		// Arrange
		using var store = CreateCounterStore(1);
		var transformedValues = new List<string>();
		var query = from value in store
					select $"Value: {value}";

		// Act
		using var subscription = query.Subscribe(value => transformedValues.Add(value));
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());

		// Assert
		transformedValues.ShouldBe(new[] { "Value: 1", "Value: 2", "Value: 3" });
	}

	[Fact]
	public void WhereAndSelect_ShouldFilterAndTransform() {
		// Arrange
		using var store = CreateCounterStore(0);
		var results = new List<int>();
		var query = from value in store
					where value % 2 == 0
					select value / 2;

		// Act
		using var subscription = query.Subscribe(value => results.Add(value));
		store.Dispatch(new Incremented()); // 1 - odd, filtered out
		store.Dispatch(new Incremented()); // 2 - even, becomes 1
		store.Dispatch(new Incremented()); // 3 - odd, filtered out
		store.Dispatch(new Incremented()); // 4 - even, becomes 2

		// Assert
		results.ShouldBe(new[] { 0, 1, 2 }); // 0/2=0, 2/2=1, 4/2=2
	}

	[Fact]
	public void Skip_ShouldSkipFirstValues() {
		// Arrange
		using var store = CreateCounterStore(0);
		var values = new List<int>();

		// Act
		using var subscription = store.Skip(2).Subscribe(value => values.Add(value));
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());

		// Assert
		values.ShouldBe(new[] { 2, 3 }); // Skipped 0 and 1
	}

	[Fact]
	public void Take_ShouldTakeFirstValues() {
		// Arrange
		using var store = CreateCounterStore(0);
		var values = new List<int>();

		// Act
		using var subscription = store.Take(3).Subscribe(value => values.Add(value));
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());

		// Assert
		values.ShouldBe(new[] { 0, 1, 2 }); // Took only first 3
	}

	[Fact]
	public void DistinctUntilChanged_ShouldOnlyEmitWhenValueChanges() {
		// Arrange
		using var store = CreateCounterStore(0);
		var values = new List<int>();

		// Act
		using var subscription = store.DistinctUntilChanged().Subscribe(value => values.Add(value));
		store.Dispatch(new SetValue(1));
		store.Dispatch(new SetValue(1)); // Same value, should not emit
		store.Dispatch(new SetValue(2));
		store.Dispatch(new SetValue(2)); // Same value, should not emit
		store.Dispatch(new SetValue(1));

		// Assert
		values.ShouldBe(new[] { 0, 1, 2, 1 });
	}

	[Fact]
	public void Scan_ShouldAccumulateValues() {
		// Arrange
		using var store = CreateCounterStore(1);
		var accumulated = new List<int>();

		// Act
		using var subscription = store
			.Scan((acc, value) => acc + value)
			.Subscribe(value => accumulated.Add(value));
		store.Dispatch(new Incremented()); // 2
		store.Dispatch(new Incremented()); // 3

		// Assert
		accumulated.ShouldBe(new[] { 1, 3, 6 }); // 1, 1+2=3, 3+3=6
	}

	[Fact]
	public void Sample_ShouldSampleAtIntervals() {
		// Arrange
		using var store = CreateCounterStore(0);
		var sampledValues = new List<int>();

		// Act
		using var subscription = store
			.Sample(TimeSpan.FromMilliseconds(50))
			.Subscribe(value => sampledValues.Add(value));

		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());
		Thread.Sleep(60);
		store.Dispatch(new Incremented());
		Thread.Sleep(60);

		// Assert
		sampledValues.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void Buffer_ShouldBufferValues() {
		// Arrange
		using var store = CreateCounterStore(0);
		var bufferedValues = new List<IList<int>>();

		// Act
		using var subscription = store
			.Buffer(2)
			.Subscribe(buffer => bufferedValues.Add(buffer));
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());
		store.Dispatch(new Incremented());

		// Assert
		bufferedValues.Count.ShouldBe(2);
		bufferedValues[0].ShouldBe(new[] { 0, 1 });
		bufferedValues[1].ShouldBe(new[] { 2, 3 });
	}

	[Fact]
	public void CombineLatest_ShouldCombineMultipleStores() {
		// Arrange
		using var store1 = CreateCounterStore(1);
		using var store2 = CreateCounterStore(10);
		var combinedValues = new List<int>();

		// Act
		using var subscription = store1
			.CombineLatest(store2, (a, b) => a + b)
			.Subscribe(value => combinedValues.Add(value));
		store1.Dispatch(new Incremented()); // 2
		store2.Dispatch(new Incremented()); // 11

		// Assert
		combinedValues.ShouldBe(new[] { 11, 12, 13 }); // 1+10=11, 2+10=12, 2+11=13
	}

	[Fact]
	public void Merge_ShouldMergeMultipleStores() {
		// Arrange
		using var store1 = CreateCounterStore(1);
		using var store2 = CreateCounterStore(100);
		var mergedValues = new List<int>();

		// Act
		using var subscription = store1
			.Merge(store2)
			.Subscribe(value => mergedValues.Add(value));
		store1.Dispatch(new Incremented());
		store2.Dispatch(new Incremented());

		// Assert
		mergedValues.ShouldContain(1);
		mergedValues.ShouldContain(2);
		mergedValues.ShouldContain(100);
		mergedValues.ShouldContain(101);
	}

	[Fact]
	public void StartWith_ShouldStartWithValue() {
		// Arrange
		using var store = CreateCounterStore(5);
		var values = new List<int>();

		// Act
		using var subscription = store
			.StartWith(0)
			.Subscribe(value => values.Add(value));
		store.Dispatch(new Incremented());

		// Assert
		values.ShouldBe(new[] { 0, 5, 6 });
	}

	[Fact]
	public void Do_ShouldExecuteSideEffect() {
		// Arrange
		using var store = CreateCounterStore(0);
		var sideEffectValues = new List<int>();
		var finalValues = new List<int>();

		// Act
		using var subscription = store
			.Do(value => sideEffectValues.Add(value))
			.Subscribe(value => finalValues.Add(value));
		store.Dispatch(new Incremented());

		// Assert
		sideEffectValues.ShouldBe(new[] { 0, 1 });
		finalValues.ShouldBe(new[] { 0, 1 });
	}

	[Fact]
	public void MultipleObservableOperators_ShouldWorkTogether() {
		// Arrange
		using var store = CreateCounterStore(0);
		var results = new List<int>();
		var query = store
			.Where(value => value > 0)
			.Select(value => value * 2)
			.DistinctUntilChanged();

		// Act
		using var subscription = query.Subscribe(value => results.Add(value));
		store.Dispatch(new Incremented()); // 1 -> 2
		store.Dispatch(new Incremented()); // 2 -> 4
		store.Dispatch(new SetValue(2));   // 2 -> 4 (duplicate, filtered)
		store.Dispatch(new Incremented()); // 3 -> 6

		// Assert
		results.ShouldBe(new[] { 2, 4, 6 });
	}
}
