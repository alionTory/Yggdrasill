using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yggdrasill.Tests.E2E;

/// <summary>
/// 강제 타임아웃과 함께 메서드를 호출하도록 돕는 클래스.
/// </summary>
public class TimeoutCaller
{
    public TimeSpan Timeout { get; set; }
    
    
    /// <summary>
    /// 타임아웃 발생 시 예외에 첨부할 메시지
    /// </summary>
    public string TimeoutMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// <paramref name="call"/>을 호출하고, <see cref="Timeout"/> 안에 응답하지 않으면 <see cref="TimeoutException"/>을 던진다.
    /// </summary>
    public async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        using var cts = new CancellationTokenSource(Timeout);
        var rpcTask = call();

        var finished = await Task.WhenAny(rpcTask, Task.Delay(Timeout));
        if (finished != rpcTask)
        {
            // 버려지는 태스크의 예외를 관측해 UnobservedTaskException을 막는다.
            _ = rpcTask.ContinueWith(
                t => Log.Progress(
                    $"타임아웃이 초과된 작업이 뒤늦게 실패: {t.Exception?.InnerException?.Message}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            throw new TimeoutException($"{Timeout.TotalSeconds}초가 지나 타임아웃 발생: {TimeoutMessage}");
        }

        return await rpcTask;
    }
    
    /// <summary>
    /// <paramref name="call"/>을 호출하고, <see cref="Timeout"/> 안에 응답하지 않으면 <see cref="TimeoutException"/>을 던진다.
    /// </summary>
    public async Task CallAsync(Func<Task> call)
    {
        await CallAsync(async () =>
        {
            await call();
            return 0;
        });
    }
}