# Yggdrasill
위그드라실 - 생태 전략 시뮬레이션 게임

## E2E 테스트
E2E 테스트는 빌드된 게임의 실행 파일을 사용한다. 따라서 빌드를 먼저 한 뒤에 E2E 테스트를 실행해야 한다.
테스트가 정상적으로 실행되려면, 전처리 심벌 DEBUG 가 정의된 채로 빌드해야 한다.

E2E 테스트 실행을 위해서는 다음 테스트 파라미터를 지정해야 한다.
1. `BuildPath`: 빌드된 게임의 실행 파일 경로를 지정한다. (리포지터리 루트 기준 상대 경로)
2. `PhotonAppVersion`: 매치메이킹 테스트 시 사용될 수 있는 앱 버전 값. Photon Realtime은 앱 버전 값이 동일한 클라이언트끼리만 매칭시킨다.
   - (모든 E2E 테스트에서 해당 파라미터로 주어진 값을 사용하는 건 아니다. 예를 들어, 자동 매칭 테스트에서는 이 값을 사용하여 매치메이킹 환경을 격리하지만, 비공개 방 매칭 테스트 시에는 실제 운영 환경에서 사용되는 게임 버전 값을 사용한다.)

로컬에서는 StandAloneTest/Yggdrasill.Tests.E2E/local.runsettings 파일을 수정하여 위 파라미터 값을 지정할 수 있다.
이후 리포지터리 루트에서 다음 명령어로 테스트를 실행한다.
```bash
dotnet test .\StandAloneTest\Yggdrasill.Tests.sln
```

CI 환경에서는 다음과 같이 지정 가능하다.
```yaml
env:
  BUILD_PATH: Builds/Yggdrasill.exe
run: >
  dotnet test 
  StandAloneTest/Yggdrasill.Tests.sln 
  --
  "TestRunParameters.Parameter(name=\"BuildPath\", value=\"$BUILD_PATH\")"
  "TestRunParameters.Parameter(name=\"PhotonAppVersion\", value=\"dev1.0.0\")"
```

CI 환경에서는 테스트 실행 시 **YGGDRASILL_E2E_GAME_ARGS 환경 변수**도 지정해야 한다.
이는 E2E 테스트 드라이버에서 게임 프로세스를 실행시킬 때, 게임 프로세스에 줄 명령행 인수 값을 지정한다.
CI 환경에서는 GUI 지원이 없는 경우가 많기 때문에, 이를 반영한 옵션을 주어야 한다.
권장 옵션은 다음과 같다.
```yaml
env:
  YGGDRASILL_E2E_GAME_ARGS: '-batchmode -nographics -screen-width 1920 -screen-height 1080'
  ...
run: >
  dotnet test 
  StandAloneTest/Yggdrasill.Tests.sln 
  ...
```

