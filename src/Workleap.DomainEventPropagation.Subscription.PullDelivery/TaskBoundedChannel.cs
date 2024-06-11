using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Workleap.DomainEventPropagation;

/// <summary>
/// Provides a channel with a bounded capacity that keeps tasks internally until they are completed and outputs them in order of completion.
/// </summary>
/// <typeparam name="T">The type of Task</typeparam>
internal class TaskBoundedChannel<T> : Channel<T> where T : Task
{
    private readonly int _bufferedCapacity;
    private readonly Channel<T> _outputChannel = Channel.CreateUnbounded<T>();
    private readonly List<T> _tasks = [];

    public TaskBoundedChannel(int bufferedCapacity)
    {
        this._bufferedCapacity = bufferedCapacity;
        this.Reader = new TaskBoundedChannelReader(this);
        this.Writer = new TaskBoundedChannelWriter(this);
    }

    private object SyncObj => this._tasks;

    public class TaskBoundedChannelReader : ChannelReader<T>
    {
        private readonly TaskBoundedChannel<T> _parent;

        public TaskBoundedChannelReader(TaskBoundedChannel<T> parent)
        {
            this._parent = parent;
        }

        public override bool CanCount => true;

        public override int Count
        {
            get
            {
                lock (this._parent.SyncObj)
                {
                    return this._parent._tasks.Count;
                }
            }
        }

        public override bool TryRead([MaybeNullWhen(false)] out T item)
        {
            return this._parent._outputChannel.Reader.TryRead(out item);
        }

        public override ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
        {
            return this._parent._outputChannel.Reader.ReadAsync(cancellationToken);
        }

        public override IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return this._parent._outputChannel.Reader.ReadAllAsync(cancellationToken);
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return this._parent._outputChannel.Reader.WaitToReadAsync(cancellationToken);
        }
    }

    public class TaskBoundedChannelWriter : ChannelWriter<T>
    {
        private readonly TaskBoundedChannel<T> _parent;
        private TaskCompletionSource _taskCompletionSource = new();

        public TaskBoundedChannelWriter(TaskBoundedChannel<T> parent)
        {
            this._parent = parent;
        }

        public override bool TryComplete(Exception? exception = null)
        {
            return false;
        }

        public override bool TryWrite(T task)
        {
            lock (this._parent.SyncObj)
            {
                if (this._parent._tasks.Count < this._parent._bufferedCapacity)
                {
                    task.ContinueWith(this.OnTaskCompletion, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    this._parent._tasks.Add(task);
                    return true;
                }

                return false;
            }
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
            }

            lock (this._parent.SyncObj)
            {
                // If there's space to write, a write is possible.
                if (this._parent._tasks.Count < this._parent._bufferedCapacity)
                {
                    return new ValueTask<bool>(true);
                }

                // There's no space, so wait until there is.
                return WaitForNextCompletedTask();

                async ValueTask<bool> WaitForNextCompletedTask()
                {
                    await this._taskCompletionSource.Task.ConfigureAwait(false);
                    return true;
                }
            }
        }

        public override async ValueTask WriteAsync(T task, CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (this._parent.SyncObj)
                {
                    if (this._parent._tasks.Count < this._parent._bufferedCapacity)
                    {
                        this._parent._tasks.Add(task);
                        task.ContinueWith(this.OnTaskCompletion, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                        return;
                    }
                }

                await this.WaitToWriteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private void OnTaskCompletion(Task task)
        {
            var typedTask = (T)task;
            lock (this._parent.SyncObj)
            {
                this._parent._tasks.Remove(typedTask);
                this._parent._outputChannel.Writer.TryWrite(typedTask);

                if (this._parent._tasks.Count < this._parent._bufferedCapacity)
                {
                    var currentTcs = this._taskCompletionSource;
                    this._taskCompletionSource = new();
                    currentTcs.TrySetResult();
                }
            }
        }
    }
}