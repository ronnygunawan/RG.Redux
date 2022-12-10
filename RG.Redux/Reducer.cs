namespace RG.Redux;

public delegate TState Reducer<TState, TEvent>(TState state, TEvent @event) where TEvent : IEvent;
