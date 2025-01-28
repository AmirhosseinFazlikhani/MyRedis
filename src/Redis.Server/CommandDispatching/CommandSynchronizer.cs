using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Redis.Server.Protocol;
using Serilog;

namespace Redis.Server.CommandDispatching;

public static class CommandSynchronizer
{
    private static bool _isStartedOnce;
    private static Task _consumingTask = Task.CompletedTask;
    private static readonly CancellationTokenSource _cancellationTokenSource = new();
    private static readonly BlockingCollection<(ICommand Command, Action<IResult>? Callback)> _commandQueue = new();
    private static readonly ConcurrentDictionary<ICommand, TaskCompletionSource> _commandWaits = new();

    public static void Start()
    {
        if (_isStartedOnce)
        {
            throw new SynchronizerAlreadyStartedOnceException();
        }

        _isStartedOnce = true;

        _consumingTask = Task.Factory.StartNew(() =>
        {
            foreach (var (command, callback) in _commandQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                try
                {
                    var response = command.Execute();
                    callback?.Invoke(response);

                    if (_commandWaits.TryRemove(command, out var completionSource))
                    {
                        completionSource.TrySetResult();
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "An unhandled exception was thrown during handling a command");
                    ExceptionDispatchInfo.Throw(exception);
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    public static void Post(ICommand command, Action<IResult>? callback = null)
    {
        _commandQueue.Add((command, callback));
    }

    public static Task PostAndWaitAsync(ICommand command, Action<IResult>? callback = null)
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _commandWaits[command] = completionSource;
        _commandQueue.Add((command, callback));
        return completionSource.Task;
    }

    public static Task PostAndWaitAsync(IEnumerable<ICommand> commands, Action<IResult>? callback = null)
    {
        commands = (commands as List<ICommand>) ?? commands.ToList();

        if (!commands.Any())
        {
            return Task.CompletedTask;
        }
        
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _commandWaits[commands.Last()] = completionSource;

        foreach (var command in commands)
        {
            _commandQueue.Add((command, callback));
        }

        return completionSource.Task;
    }

    public static void Stop()
    {
        _commandQueue.CompleteAdding();
        _cancellationTokenSource.Cancel();
        _consumingTask.Wait();
        _commandQueue.Dispose();

        foreach (var completionSources in _commandWaits.Values)
        {
            completionSources.TrySetCanceled();
        }
    }
}