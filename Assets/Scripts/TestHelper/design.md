# TestHelper
E2E 테스트를 위해, 테스트 드라이버와 게임 프로세스 간의 연결고리를 구현.

빌드에서 제외되어야 하지만, MonoBehaviour등을 씬에 등록했다가 릴리즈 빌드에서 제외하면 
1. 런타임 에러 로그가 뜰 수 있고
2. 개발 과정에서 직렬화 필드 값이 유실될 수 있으므로

해당 폴더 내 모든 코드를 빌드에서 제외할 수는 없다.

따라서 빌드에서 제외할 코드들을 별개의 어셈블리로 분리하고, defineConstraints:DEBUG를 부여한다.

## 어셈블리 구성
1. TestHelper.Protocol: 테스트 드라이버와 공유하는 코드.
    - E2E 테스트 드라이버는 유니티 비의존이므로, 유니티 의존 코드가 포함되지 않아야 한다.
    - 일부 코드가 직렬화 필드 타입으로 사용된다. 따라서 릴리즈 빌드에서 제외할 시 필드값이 유실될 위험이 있다.
    - 유니티 비의존이라 부담이 크지 않은 코드이므로, 릴리즈 빌드에 포함시킨다.
1. TestHelper.Debug: 테스트 드라이버에서 접근하지 않으면서, 릴리즈에서 제외되는 코드
2. TestHelper.CallDebug: 일부 코드가 릴리즈에서 포함되어야 하면서, Debug를 참조하는 코드
3. TestHelper.CalledByDebug: 일부 코드가 릴리즈에서 포함되어야 하면서, Debug에서 참조되는 코드

## 어셈블리 참조 구조
```text
CallDebug -> Debug -> CalledByDebug
```
그리고 Protocol은 Debug, CallDebug, CalledByDebug를 참조하지 않는다. 역방향은 가능.