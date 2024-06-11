using System.Diagnostics;
using System.Threading.Channels;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class TaskBoundedChannelTests
{
    private const int DefaultTaskDelay = 300;

    private Channel<Task>? _taskChannel;
    private int _channelCapacity;
    private bool _tryWriteResult;
    private bool _tryReadResult;
    private Task? _tryReadTaskResult;
    private bool _waitToWriteAsyncResult;
    private long _waitToWriteAsyncDelay;
    private long _writeAsyncDelay;
    private long _readAsyncDelay;
    private Task? _readAsyncResult;
    private bool _readWasCancelled;
    private List<Task>? _readAllAsyncResult;
    private long _readAllAsyncDelay;
    private bool _readAllWasCancelled;
    private bool _waitToReadAsyncResult;
    private long _waitToReadAsyncDelay;
    private bool _waitToReadWasCancelled;
    private List<Task>? _variedTasks;

    [Fact]
    public void GivenBoundedTaskChannel_WhenTryWrite_ThenReturnTrue()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);

        // When
        this.WhenTryWrite(Task.Delay(5));

        // Then
        this.ThenTryWriteResultShouldBeTrue();
    }

    [Fact]
    public void GivenMaxedCapacityBoundedTaskChannel_WhenTryWrite_ThenReturnFalse()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelAlreadyHasATaskAmountOf(10);

        // When
        this.WhenTryWrite(Task.Delay(5));

        // Then
        this.ThenTryWriteResultShouldBeFalse();
    }

    [Fact]
    public async Task GivenBoundedTaskChannel_WhenWaitToWriteAsync_ThenReturnImmediately()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);

        // When
        await this.WhenWaitToWriteAsync();

        // Then
        this.ThenWaitToWriteAsyncResultShouldBeTrue();
        this.ThenWaitToWriteAsyncResultShouldBeImmediate();
    }

    [Fact]
    public async Task GivenMaxedCapacityBoundedTaskChannel_WhenWaitToWriteAsync_ThenReturnAfterRead()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelAlreadyHasATaskAmountOf(10);

        // When
        await this.WhenWaitToWriteAsync();

        // Then
        this.ThenWaitToWriteAsyncResultShouldBeTrue();
        this.ThenWaitToWriteAsyncResultShouldWaitForTasks();
    }

    [Fact]
    public async Task GivenBoundedTaskChannel_WhenWriteAsync_ThenReturnImmediately()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);

        // When
        await this.WhenWriteAsync();

        // Then
        this.ThenWriteAsyncResultShouldBeImmediate();
    }

    [Fact]
    public async Task GivenMaxedCapacityBoundedTaskChannel_WhenWriteAsync_ThenReturnAfterRead()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelAlreadyHasATaskAmountOf(10);

        // When
        await this.WhenWriteAsync();

        // Then
        this.ThenWriteAsyncResultShouldWaitForTasks();
    }

    [Fact]
    public void GivenEmptyBoundedTaskChannel_WhenTryRead_ThenReturnFalse()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);

        // When
        this.WhenTryRead();

        // Then
        this.ThenTryReadResultShouldBeFalse();
        this.ThenTryReadTaskResultShouldBeNull();
    }

    [Fact]
    public void GivenNonEmptyBoundedTaskChannel_WhenTryRead_ThenReturnFalse()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelAlreadyHasATaskAmountOf(1);

        // When
        this.WhenTryRead();

        // Then
        this.ThenTryReadResultShouldBeFalse();
        this.ThenTryReadTaskResultShouldBeNull();
    }

    [Fact]
    public async Task GivenEmptyBoundedTaskChannel_WhenReadAsync_ThenReturnNothingAndExpire()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);

        // When
        await this.WhenReadAsync();

        // Then
        this.ThenReadAsyncShouldNotReturnTask();
        this.ThenReadAsyncResultShouldExpire();
    }

    [Fact]
    public async Task GivenNonEmptyBoundedTaskChannel_WhenReadAsync_ThenReturnAfterTaskDelay()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelAlreadyHasATaskAmountOf(1);

        // When
        await this.WhenReadAsync();

        // Then
        this.ThenReadAsyncShouldReturnTask();
        this.ThenReadAsyncResultShouldWaitForTasks();
    }

    [Fact]
    public async Task GivenEmptyBoundedTaskChannel_WhenReadAllAsync_ThenReturnNothingAndExpire()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);

        // When
        await this.WhenReadAllAsync();

        // Then
        this.ThenReadAllAsyncShouldNotReturnTask();
        this.ThenReadAllAsyncResultShouldExpire();
    }

    [Fact]
    public async Task GivenNonEmptyBoundedTaskChannel_WhenReadAllAsync_ThenReturnAfterTaskDelay()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelAlreadyHasATaskAmountOf(1);

        // When
        await this.WhenReadAllAsync();

        // Then
        this.ThenReadAllAsyncShouldReturnTasks();
        this.ThenReadAllAsyncResultShouldWaitForTasks();
    }

    [Fact]
    public async Task GivenEmptyBoundedTaskChannel_WhenWaitToReadAsync_ThenReturnNothingAndExpire()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);

        // When
        await this.WhenWaitToReadAsync();

        // Then
        this.ThenWaitToReadAsyncShouldBeFalse();
        this.ThenWaitToReadAsyncResultShouldExpire();
    }

    [Fact]
    public async Task GivenNonEmptyBoundedTaskChannel_WhenWaitToReadAsync_ThenReturnAfterTaskDelay()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelAlreadyHasATaskAmountOf(1);

        // When
        await this.WhenWaitToReadAsync();

        // Then
        this.ThenWaitToReadAsyncShouldBeTrue();
        this.ThenWaitToReadAsyncResultShouldWaitForTasks();
    }

    [Fact]
    public async Task GivenNonEmptyBoundedTaskChannel_WhenReadAllAsync_ThenReturnTasksInOrderOfCompletion()
    {
        // Given
        this.GivenBoundedTaskChannelLimitedTo(10);
        this.GivenChannelHasTasksWithVariedCompletionDelay();

        // When
        await this.WhenReadAllAsync(DefaultTaskDelay * 10);

        // Then
        this.ThenReadAllAsyncShouldReturnTasks();
        this.ThenReadAllAsyncResultShouldBeOrderedTasks();
    }

    private void GivenBoundedTaskChannelLimitedTo(int capacity)
    {
        this._channelCapacity = capacity;
        this._taskChannel = new TaskBoundedChannel<Task>(this._channelCapacity);
    }

    private void GivenChannelAlreadyHasATaskAmountOf(int existingTaskAmount)
    {
        for (var i = 0; i < existingTaskAmount; i++)
        {
            this._taskChannel!.Writer.TryWrite(Task.Delay(DefaultTaskDelay));
        }
    }

    private void GivenChannelHasTasksWithVariedCompletionDelay()
    {
        this._variedTasks =
        [
            Task.Delay(DefaultTaskDelay * 4),
            Task.Delay(DefaultTaskDelay * 3),
            Task.Delay(DefaultTaskDelay * 1),
            Task.Delay(DefaultTaskDelay * 5),
            Task.Delay(DefaultTaskDelay * 2)
        ];

        foreach (var task in this._variedTasks)
        {
            this._taskChannel!.Writer.TryWrite(task);
        }
    }

    private void WhenTryWrite(Task task)
    {
        this._tryWriteResult = this._taskChannel!.Writer.TryWrite(task);
    }

    private async Task WhenWriteAsync()
    {
        var stopWatch = Stopwatch.StartNew();
        await this._taskChannel!.Writer.WriteAsync(Task.Delay(DefaultTaskDelay));
        this._writeAsyncDelay = stopWatch.ElapsedMilliseconds;
    }

    private async Task WhenWaitToWriteAsync()
    {
        var stopWatch = Stopwatch.StartNew();
        this._waitToWriteAsyncResult = await this._taskChannel!.Writer.WaitToWriteAsync();
        this._waitToWriteAsyncDelay = stopWatch.ElapsedMilliseconds;
    }

    private void WhenTryRead()
    {
        this._tryReadResult = this._taskChannel!.Reader.TryRead(out this._tryReadTaskResult);
    }

    private async Task WhenReadAsync()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(DefaultTaskDelay * 2);

        var stopWatch = Stopwatch.StartNew();
        try
        {
            this._readAsyncResult = await this._taskChannel!.Reader.ReadAsync(cts.Token);
            this._readAsyncDelay = stopWatch.ElapsedMilliseconds;
        }
        catch (OperationCanceledException)
        {
            this._readWasCancelled = true;
            this._readAsyncDelay = stopWatch.ElapsedMilliseconds;
        }
    }

    private async Task WhenReadAllAsync(int maxDelay = DefaultTaskDelay * 2)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(maxDelay);

        var stopWatch = Stopwatch.StartNew();
        try
        {
            await foreach (var task in this._taskChannel!.Reader.ReadAllAsync(cts.Token))
            {
                this._readAllAsyncResult ??= [];
                this._readAllAsyncResult.Add(task);
            }

            this._readAllAsyncDelay = stopWatch.ElapsedMilliseconds;
        }
        catch (OperationCanceledException)
        {
            this._readAllWasCancelled = true;
            this._readAllAsyncDelay = stopWatch.ElapsedMilliseconds;
        }
    }

    private async Task WhenWaitToReadAsync()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(DefaultTaskDelay * 2);

        var stopWatch = Stopwatch.StartNew();
        try
        {
            this._waitToReadAsyncResult = await this._taskChannel!.Reader.WaitToReadAsync(cts.Token);
            this._waitToReadAsyncDelay = stopWatch.ElapsedMilliseconds;
        }
        catch (OperationCanceledException)
        {
            this._waitToReadWasCancelled = true;
            this._waitToReadAsyncDelay = stopWatch.ElapsedMilliseconds;
        }
    }

    private void ThenTryWriteResultShouldBeFalse()
    {
        Assert.False(this._tryWriteResult);
    }

    private void ThenTryWriteResultShouldBeTrue()
    {
        Assert.True(this._tryWriteResult);
    }

    private void ThenWaitToWriteAsyncResultShouldBeTrue()
    {
        Assert.True(this._waitToWriteAsyncResult);
    }

    private void ThenWaitToWriteAsyncResultShouldBeImmediate()
    {
        // Using 20% of the default task delay as a threshold for "immediate"
        Assert.True(this._waitToWriteAsyncDelay < DefaultTaskDelay * 0.2);
    }

    private void ThenWaitToWriteAsyncResultShouldWaitForTasks()
    {
        // Using 90% of the default task delay as a threshold for "waiting"
        Assert.True(this._waitToWriteAsyncDelay >= DefaultTaskDelay * 0.9);
    }

    private void ThenWriteAsyncResultShouldBeImmediate()
    {
        // Using 20% of the default task delay as a threshold for "immediate"
        Assert.True(this._writeAsyncDelay < DefaultTaskDelay * 0.2);
    }

    private void ThenWriteAsyncResultShouldWaitForTasks()
    {
        // Using 90% of the default task delay as a threshold for "waiting"
        Assert.True(this._writeAsyncDelay >= DefaultTaskDelay * 0.9);
    }

    private void ThenTryReadResultShouldBeFalse()
    {
        Assert.False(this._tryReadResult);
    }

    private void ThenTryReadTaskResultShouldBeNull()
    {
        Assert.Null(this._tryReadTaskResult);
    }

    private void ThenReadAsyncShouldNotReturnTask()
    {
        Assert.Null(this._readAsyncResult);
    }

    private void ThenReadAsyncShouldReturnTask()
    {
        Assert.NotNull(this._readAsyncResult);
    }

    private void ThenReadAsyncResultShouldExpire()
    {
        Assert.True(this._readWasCancelled);
    }

    private void ThenReadAsyncResultShouldWaitForTasks()
    {
        // Using 90% of the default task delay as a threshold for "waiting"
        Assert.True(this._readAsyncDelay >= DefaultTaskDelay * 0.9);
    }

    private void ThenReadAllAsyncShouldNotReturnTask()
    {
        Assert.Null(this._readAllAsyncResult);
    }

    private void ThenReadAllAsyncShouldReturnTasks()
    {
        Assert.NotNull(this._readAllAsyncResult);
        Assert.NotEmpty(this._readAllAsyncResult);
    }

    private void ThenReadAllAsyncResultShouldExpire()
    {
        Assert.True(this._readAllWasCancelled);
    }

    private void ThenReadAllAsyncResultShouldWaitForTasks()
    {
        // Using 90% of the default task delay as a threshold for "waiting"
        Assert.True(this._readAllAsyncDelay >= DefaultTaskDelay * 0.9);
    }

    private void ThenReadAllAsyncResultShouldBeOrderedTasks()
    {
        Assert.Same(this._variedTasks![2], this._readAllAsyncResult![0]);
        Assert.Same(this._variedTasks[4], this._readAllAsyncResult[1]);
        Assert.Same(this._variedTasks[1], this._readAllAsyncResult[2]);
        Assert.Same(this._variedTasks[0], this._readAllAsyncResult[3]);
        Assert.Same(this._variedTasks[3], this._readAllAsyncResult[4]);
    }

    private void ThenWaitToReadAsyncShouldBeFalse()
    {
        Assert.False(this._waitToReadAsyncResult);
    }

    private void ThenWaitToReadAsyncShouldBeTrue()
    {
        Assert.True(this._waitToReadAsyncResult);
    }

    private void ThenWaitToReadAsyncResultShouldExpire()
    {
        Assert.True(this._waitToReadWasCancelled);
    }

    private void ThenWaitToReadAsyncResultShouldWaitForTasks()
    {
        // Using 90% of the default task delay as a threshold for "waiting"
        Assert.True(this._waitToReadAsyncDelay >= DefaultTaskDelay * 0.9);
    }
}
