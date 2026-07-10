---
_layout: landing
---

# Yggdrasill API 문서

이 문서는 `Assets/Photon` 및 `Assets/QuantumUser` 폴더 아래의 C# 코드에서 자동 생성된 API 레퍼런스입니다.

## 구성

- **Photon** — Photon Realtime, Quantum, Quantum Menu 등 네트워킹/시뮬레이션 라이브러리 코드
- **QuantumUser** — 이 프로젝트에서 작성한 Quantum 시뮬레이션/뷰/에디터 코드

## 시작하기

왼쪽 상단의 **[API 문서](api/index.md)** 메뉴에서 네임스페이스별 타입 목록을 확인할 수 있습니다.

## 문서 재생성

```bash
# Docs/docfx 폴더에서 실행
docfx metadata   # 소스 코드에서 API 메타데이터(YAML) 추출
docfx build      # 정적 HTML 사이트 생성 (_site)
docfx serve _site  # 로컬 미리보기 (http://localhost:8080)

# 또는 한 번에
docfx docfx.json --serve
```
