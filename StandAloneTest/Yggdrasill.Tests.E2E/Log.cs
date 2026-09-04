using NUnit.Framework;

namespace Yggdrasill.Tests.E2E;

/// <summary>
/// E2E 테스트의 진단용 로그를 남긴다.
/// </summary>
public static class Log
{
    /// <summary>
    /// <paramref name="message"/>를 실시간 출력 스트림, 테스트 별 버퍼 양쪽에 로그를 남긴다.
    /// </summary>
    /// <remarks>
    /// 실시간 출력 스트림과 달리, 테스트 별 버퍼는 로그가 어느 테스트에 속하는지를 구분한다.
    /// 그러나 테스트 별 버퍼에 남긴 로그는, 테스트 호스트가 (--blame-hang 타임아웃 등으로) 중단 시 유실될 수 있다.
    /// 또한 [SetUp]에서 버퍼에 로그 출력 시에도, 특정 테스트에 속하지 않으므로, 테스트 보고서에 기록되지 않을 수 있다.
    /// 로그를 빠짐없이 수집하려면 전자가, 로그가 속한 테스트를 판별하려면 후자가 필요하므로, 둘 다에 쓸 필요가 있다.
    /// </remarks>
    public static void Write(string message)
    {
        TestContext.Progress.WriteLine(message);
        TestContext.Out.WriteLine(message);
    }

    /// <summary>
    /// 실시간 출력 스트림에만 로그를 출력.
    /// </summary>
    public static void Progress(string message)
    {
        TestContext.Progress.WriteLine(message);
    }
}
