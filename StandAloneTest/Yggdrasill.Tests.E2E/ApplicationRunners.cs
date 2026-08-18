using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.E2eTests;

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
    public static async Task<IEnumerable<TResult>> ForEachParallel<TResult>(this IEnumerable<ApplicationRunner> runners,
        Func<ApplicationRunner, Task<TResult>> func)
    {
        return await Task.WhenAll(
            runners.Select(func)
        );
    }
    
    /// <summary>
    /// <paramref name="runners"/>의 각 요소에 대해 <paramref name="func"/>를 병렬로 실행한다.
    /// </summary>
    /// <returns>
    /// 반환 결과에서 <paramref name="runners"/>에서의 순서가 보장된다.
    /// </returns>
    public static Task ForEachParallel(this IEnumerable<ApplicationRunner> runners,
        Func<ApplicationRunner, Task> func)
    {
        return Task.WhenAll(
            runners.Select(func)
        );
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
}