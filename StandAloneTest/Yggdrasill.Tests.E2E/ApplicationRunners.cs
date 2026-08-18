using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Tests.E2eTests;

/// <summary>
/// 병렬 실행 중 발생한 모든 예외 정보를 담고 있다.
/// </summary>
public class ParallelRunException(IReadOnlyList<Exception?> errors) : Exception(BuildMessage(errors))
{
    public IReadOnlyList<Exception?> SubExceptions { get; } = errors.ToArray();

    private static string BuildMessage(IReadOnlyList<Exception?> errors)
    {
        var realErrorCount =errors.Count(error => error != null);
        var result = $"{errors.Count}개 중 {realErrorCount}개 실패:";

        foreach (var (error, index) in errors.Select((error, index) => (error, index)))
        {
            if (error != null)
            {
                result += Environment.NewLine;
                result += $"  - {index}번 원소에서 예외 {error.GetType().Name} 발생: {error.Message}";
            }
        }

        return result;
    }
}

/**
 * <see cref="ApplicationRunner"/>를 다룰 때 유용한 메서드들.
 */
public static class ApplicationRunners
{
    /// <summary>
    /// <paramref name="count"/>개의 <see cref="ApplicationRunner"/>으로 구성된 리스트를 생성한다.
    /// </summary>
    /// <param name="count">0 이상이여야 함.</param>
    public static async Task<ImmutableList<ApplicationRunner>> StartRunners(int count)
    {
        var applications = await Task.WhenAll(
            Enumerable.Range(0, count)
                .Select(_ => ApplicationRunner.StartAsync())
        );
        return applications.ToImmutableList();
    }

    /// <summary>
    /// <paramref name="runners"/>의 각 요소에 대해 <paramref name="func"/>를 병렬로 실행하고, 결과를 모아 반환한다.
    /// </summary>
    /// <returns>
    /// 반환 결과에서 <paramref name="runners"/>에서의 순서가 보장된다.
    /// </returns>
    /// <exception cref="ParallelRunException">
    /// <paramref name="func"/> 병렬 실행 중 하나 이상의 예외 발생 시 이 예외가 던져진다.
    /// </exception>
    public static async Task<IEnumerable<TResult>> ForEachParallel<TResult>(this IEnumerable<ApplicationRunner> runners,
        Func<ApplicationRunner, Task<TResult>> func)
    {
        var taskExecutionResults = await Task.WhenAll(
            runners.Select(async runner =>  
            {
                try
                {
                    return (successResult: await func(runner), error: null);
                }
                catch (Exception ex)
                {
                    return (successResult: default(TResult), error: ex);
                }
            })
        );
        
        var errors = taskExecutionResults.Select(x => x.error).ToArray();
        if (errors.Any(error => error != null))
            throw new ParallelRunException(errors);

        return taskExecutionResults.Select(x => x.successResult!);
    }
    
    /// <summary>
    /// <paramref name="runners"/>의 각 요소에 대해 <paramref name="func"/>를 병렬로 실행한다.
    /// </summary>
    /// <returns>
    /// 반환 결과에서 <paramref name="runners"/>에서의 순서가 보장된다.
    /// </returns>
    /// <exception cref="ParallelRunException">
    /// <paramref name="func"/> 병렬 실행 중 하나 이상의 예외 발생 시 이 예외가 던져진다.
    /// </exception>
    public static Task ForEachParallel(this IEnumerable<ApplicationRunner> runners,
        Func<ApplicationRunner, Task> func)
    {
        return runners.ForEachParallel(async runner =>
        {
            await func(runner);
            return 0;
        });
    }

    /// <summary>
    /// <paramref name="runners"/>에 대해 <paramref name="predicate"/>를 병렬 실행하여,
    /// <paramref name="runners"/> 중 await <paramref name="predicate"/> 결과가 참인 원소만 골라 반환.
    /// </summary>
    public static async Task<IEnumerable<ApplicationRunner>> WhereParallel(this IEnumerable<ApplicationRunner> runners,
        Func<ApplicationRunner, Task<bool>> predicate)
    {
        var runnerBoolPairs = await runners.ForEachParallel(async runner => (runner:runner, boolResult: await predicate(runner)));

        return runnerBoolPairs.Where(pair => pair.boolResult).Select(pair => pair.runner);
    }

    public static async Task AssertThat(this IEnumerable<ApplicationRunner> runners,
        Func<ApplicationRunner, Task<TestDelegate>> func, IResolveConstraint constraint, string message = "")
    {
        var taskExecutionResults = await runners.ForEachParallel(func);
        Assert.Multiple(() =>
        {
            foreach (var (result, index) in taskExecutionResults.Select((result, index) => (result, index)))
            {
                Assert.That(result, constraint, $"{index}번 Assert 실패: "+message);
            }
        });
    }
}