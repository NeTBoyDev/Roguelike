using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public interface IEventParameters { }

public class SpawnEnemiesParameters : IEventParameters
{
    public int EnemyCount { get; }
    public SpawnEnemiesParameters(int enemyCount) => EnemyCount = enemyCount;
}

public class DelayParameters : IEventParameters
{
    public float DelaySeconds { get; }
    public DelayParameters(float delaySeconds) => DelaySeconds = delaySeconds;
}

public class GameEvent
{
    public GameEvent NextEvent { get; private set; }

    private readonly Func<CancellationToken, IEventParameters, UniTask> executionCallback;
    private readonly IEventParameters parameters;

    public GameEvent(Func<CancellationToken, IEventParameters, UniTask> callback, IEventParameters parameters = null)
    {
        executionCallback = callback ?? throw new ArgumentNullException(nameof(callback));
        this.parameters = parameters;
    }

    public GameEvent ChainWith(GameEvent nextEvent)
    {
        NextEvent = nextEvent;
        return nextEvent;
    }

    public async UniTask Execute(CancellationToken cancellationToken = default)
    {
        try
        {
            await executionCallback(cancellationToken, parameters);
            if (NextEvent != null && !cancellationToken.IsCancellationRequested)
            {
                await NextEvent.Execute(cancellationToken);
            }
        }

        finally { }
    }
}


// Event chain builder

public class EventBuilder
{
    private GameEvent firstEvent;
    private GameEvent lastEvent;

    // Original method for adding a ready-made Event

    public EventBuilder AddEvent(GameEvent newEvent)
    {
        if (firstEvent == null)
        {
            firstEvent = lastEvent = newEvent;
        }
        else
        {
            lastEvent.ChainWith(newEvent);
            lastEvent = newEvent;
        }
        return this;
    }

    public EventBuilder AddEvent(Action action)
    {
        var newEvent = new GameEvent(async (token, _) =>
        {
            action();
            await UniTask.CompletedTask;
        });
        return AddEvent(newEvent);
    }

    public EventBuilder AddEvent(Action<object> action)
    {
        var newEvent = new GameEvent(async (token, _) =>
        {
            action(null);
            await UniTask.CompletedTask;
        });
        return AddEvent(newEvent);
    }

    public EventBuilder AddEvent(Func<UniTask> asyncAction)
    {
        var newEvent = new GameEvent(async (token, _) =>
        {
            await asyncAction();
        });
        return AddEvent(newEvent);
    }

    public EventBuilder AddEvent(Func<CancellationToken, UniTask> asyncActionWithToken)
    {
        var newEvent = new GameEvent( async (token, _) =>
        {
            await asyncActionWithToken(token);
        });
        return AddEvent(newEvent);
    }

    public EventBuilder AddEvent<T>(Func<T> function)
    {
        var newEvent = new GameEvent(async (token, _) =>
        {
            T result = function();
            Debug.Log($"Function execution result: {result}");
            await UniTask.CompletedTask;
        });
        return AddEvent(newEvent);
    }
    public EventBuilder AddDelay(float seconds)
    {
        var delayEvent = new GameEvent(async (token, _) =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
        });

        return AddEvent(delayEvent);
    }

    public GameEvent Build()
    {
        if (firstEvent == null)
            throw new InvalidOperationException("No events added to the chain.");
        return firstEvent;
    }

    public static EventBuilder Create() => new();
}