> **상태** : 코드 구현됨 (작업 순서 1, 3). 프리팹 · 씬 구성(4, 5)과 인게임 테스트 전. 결정 사항 D1 ~ D8은 기본안이다. 확정하면 표시를 지운다 (9장)
> **기준** : 브랜치 `origin/feature/systemRefactoring`, 커밋 `cd1e9c3` + 작업 트리. 기존 레버는 `feature/Lever`(PR #11)로 들어온 `Assets/Scripts/Lever/Lever.cs`, `Assets/Prefabs/Lever.prefab`, `Assets/Animation/Lever.controller`
> **목적** : 레버 하나로 조작하는 설비 C를 만든다. 설비 C가 고장 나면 플레이어가 레버를 당기고, **당길 때마다 n% 확률로 고장이 회복**된다.
> **규칙** : 「게임 진행 규칙」에서 설비 C는 [미정]이다. 이 문서의 규칙을 확정하면 11장 목록대로 규칙 문서에 옮긴다.

## 1. 배경

### 요구 사항
- 설비 C가 고장 나면 레버를 당긴다.
- 당길 때마다 n% 확률로 고장이 회복된다. 실패하면 다시 당긴다.
- A(패턴 맞추기), B(소리로 위치 찾기)와 달리 판단이 거의 없는 설비다. 동시 고장 때 "빨리 끝낼 수 있지만 운이 섞인" 선택지가 된다.

### 기존 레버 (`feature/Lever`)

| 항목 | 현재 |
| --- | --- |
| `Lever.cs` | `IInteractable` 구현. `Interact()`를 부를 때마다 `isDown`을 **토글**하고 Animator의 `IsDown` bool을 바꾼다. 누구에게도 알리지 않는다 |
| 초기화 | `Start()`에서 Animator를 찾고 `IsDown`을 적용한다. 설비의 `Initialize()` 패턴이 아니다 |
| `Lever.prefab` | 루트 `Lever`(Animator + Lever) 아래 `HandlePivot` / `Handle` / `Sphere`(모두 **Button 레이어**, 콜라이더 있음)와 `Base`(Default 레이어) |
| `Lever.controller` | 상태 `LeverUp`(기본, 손잡이 z = +45°) ↔ `LeverDown`(z = −45°). `IsDown` true / false로 전환, **전환 시간 0.25초**, Exit Time 없음 |
| 애니메이션 클립 | 키프레임 하나짜리 자세 클립. 움직임은 상태 전환 블렌드(0.25초)로 만들어진다 |
| 소리 | 없음 |
| 사용 씬 | `LeverTestScene` |

- 손잡이 콜라이더가 Button 레이어이고 루트에 `IInteractable`이 있으므로, **`FirstPersonController`로 지금 바로 클릭된다** (`GetComponentInParent`). 컨트롤러는 수정할 필요가 없다.
- 부족한 것 : ① 당겼다는 알림(이벤트), ② "당기기 한 번"의 정의(지금은 토글이라 두 번 눌러야 원위치), ③ 동작 중 연타 방지, ④ 당기는 소리.

### 설비 공통 구조와의 관계 (현재 코드)

| 항목 | 내용 | 설비 C에 주는 영향 |
| --- | --- | --- |
| `Facility` | `Initialize / IsFault / MakeFault / Clear`, `OnFacilityInteracted(bool isFault, int id)` | 그대로 상속 |
| `FacilityManager.OnFacilityInteracted` | 수리되면 `_facilities[facilityID].Clear()` | **`facilityID`가 `_facilities` 배열 인덱스와 같아야 한다.** C는 인덱스 2, `facilityID = 2` |
| `FacilityManager.OnFaultMade` | (임시 코드) 설비 중 하나를 무작위로 골라 `MakeFault()` | 배열에 넣으면 C도 뽑힌다 |
| `MainPanelDisplay` | `Main Panel Display` 프리팹에 **설비 C 슬롯이 이미 있다** (슬롯 3개) | 인덱스만 맞추면 추가 작업 없음 |
| A, B의 고장 판정 | 장치 상태 ≠ 목표 로 계산 | C는 장치에 "맞출 상태"가 없으므로 **고장 플래그**로 관리한다 (D8) |

## 2. 범위

| 포함 | 제외 (후속 작업) |
| --- | --- |
| `Lever.cs` 수정 : 당기기 한 번 동작(내려갔다 자동 복귀), 당김 이벤트, 연타 방지, 당기는 소리 | 확률 보정(연속 실패 보장) (D5) |
| `FacilityC.cs` : 고장 플래그, 확률 회복 | FeedbackManager의 성공음 · 사이렌 (공통 연출) |
| `FacilityC.prefab` 구성, 테스트 씬 등록 | 레버 조준 하이라이트 |
| 규칙 문서에 옮길 내용 정리 (11장) | 설비 C 배치 확정 (D7, [미정]) |

## 3. 조작 방식 : "당기기 한 번" (D1)

| 방안 | 동작 | 판단 |
| --- | --- | --- |
| **A. 스프링 레버** | 클릭 한 번 → 레버가 내려갔다가 **자동으로 올라온다.** 한 번 당길 때 확률 판정 한 번 | **채택.** "당긴다"는 요구와 맞고, 클릭 한 번 = 시도 한 번이라 알기 쉽다 |
| B. 토글 유지, 내려갈 때만 판정 | 지금처럼 클릭마다 위 / 아래. 내려갈 때만 판정 | 시도 한 번에 클릭 두 번. 시간 압박 게임에서 조작 비용이 크다 |
| C. 토글 유지, 움직일 때마다 판정 | 위 / 아래 모두 판정 | 조작은 빠르지만 "당긴다"는 느낌이 약하다 |

### 한 번 당길 때의 흐름 (방안 A)

```
Interact() (클릭)
├─ 당기는 중이면 무시                          ← 연타 방지
└─ Pull 코루틴 시작
    ├─ IsDown = true, 당기는 소리               → 0.25초 동안 내려간다 (Animator 전환)
    ├─ pullDuration(0.25초) 대기
    ├─ OnPulled 이벤트                          ← 끝까지 내려간 순간 확률 판정 (D2)
    ├─ holdDuration(0.1초) 대기                 ← 아래에 잠깐 머문다
    ├─ IsDown = false                           → 0.25초 동안 올라온다
    ├─ pullDuration(0.25초) 대기
    └─ 당기기 끝 (다시 클릭 가능)
```

- 한 번 당기는 데 약 **0.6초**. 이 값이 기대 수리 시간을 정한다 (8장).
- `pullDuration`은 Animator 전환 시간(0.25초)과 맞춘다. 전환 시간을 바꾸면 이 값도 같이 바꾼다.

## 4. Lever 변경

기존 클래스 이름과 프리팹을 그대로 쓰고 **`Lever.cs`를 고친다** (D6). 새 레버 클래스를 따로 만들면 프리팹이 둘로 갈라진다. `feature/Lever` 작성자와 변경 내용을 공유한다.

### 인스펙터 필드 (안)

| 필드 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| `pullDuration` | float | 0.25 | 초. 내려가는 / 올라오는 시간. Animator 전환 시간과 같게 |
| `holdDuration` | float | 0.1 | 초. 아래에 머무는 시간 |
| `pullSource` | AudioSource | (같은 오브젝트) | 당기는 소리 '철컹'. clip에 지정. 비어 있으면 `GetComponent` |

### 내부 상태

| 상태 | 설명 |
| --- | --- |
| `_animator : Animator` | `Awake`에서 캐시 (지금의 `Start` 대신. 설비 초기화 순서와 상관없게) |
| `_isPulling : bool` | 당기는 중. true면 `Interact()` 무시 |
| `_pullRoutine : Coroutine` | 진행 중인 당기기. 초기화 · 비활성화 때 멈춘다 |

### 공개 API (안)

| 멤버 | 설명 |
| --- | --- |
| `event Action OnPulled` | 끝까지 내려간 순간 한 번. 소속 설비가 구독한다 |
| `bool IsPulling` | 당기는 중인지 |
| `void Interact()` | `IInteractable`. 당기는 중이 아니면 3장 흐름 시작 |
| `void ResetLever()` | 코루틴을 멈추고 올라간 상태로 즉시 되돌린다. 게임 재시작 · 비활성화용. **수리(`Clear`) 때는 부르지 않는다** (아래 주의) |

### 주의
- **수리 직후 레버를 강제로 올리지 않는다.** 회복 판정은 `OnPulled` 안에서 일어나고, `FacilityManager`는 그 자리에서 바로 `FacilityC.Clear()`를 부른다. `Clear`가 레버까지 초기화하면 내려가 있던 손잡이가 순간이동한다. `Clear`는 고장 플래그만 지우고, 레버는 남은 흐름대로 올라오게 둔다.
- `OnDisable`에서 코루틴을 멈추고 `_isPulling = false`로 둔다. 다시 켜졌을 때 영원히 잠기지 않게 한다.
- 설비에 소속되지 않은 레버(`LeverTestScene`)도 클릭하면 내려갔다 올라온다. 이벤트 구독자가 없을 뿐이다.
- `Update()` 빈 함수와 기본 주석은 지운다.

## 5. FacilityC 명세

> **변경됨** : `FacilityC`는 「설비 고장 플래그 · 수리 후 초기화 계획」의 기반 클래스 구조로 바뀌었다. 확률 판정은 `IsGoalReached()`, 목표 생성 · 장치 초기화는 비어 있다. 실패한 당김은 더 이상 `OnFacilityInteracted`를 보내지 않는다 (알림은 수리 때만).

### 인스펙터 필드 (안)

| 필드 | 타입 | 기본값 | 설명 |
| --- | --- | --- | --- |
| `facilityID` | int | 2 | (`Facility` 공통) `FacilityManager._facilities` 인덱스와 같게 |
| `lever` | Lever | — | 조작할 레버 |
| `repairChance` | float (Range 0 ~ 1) | 0.25 | 한 번 당길 때 회복 확률 (n%). 8장 |
| `isFault` | bool | — | Observation. 인스펙터 확인용 (A, B와 같은 패턴) |

### 동작

| 메서드 | 동작 |
| --- | --- |
| `Initialize()` | `_isFault = false`, `lever.OnPulled += OnLeverPulled` |
| `IsFault()` | `_isFault`를 그대로 돌려준다 (D8) |
| `MakeFault()` | `_isFault = true`. 레버는 건드리지 않는다 (「게임 진행 규칙」 7장 "장치는 현재 상태를 유지") |
| `Clear()` | `_isFault = false`. **레버는 건드리지 않는다** (4장 주의) |
| `OnLeverPulled()` | 아래 참고 |

```
OnLeverPulled
├─ 고장이 아니면 끝                          ← 정상 설비 조작은 아무 일도 없다 (「게임 진행 규칙」 6장)
├─ Random.value < repairChance 이면 _isFault = false
└─ OnFacilityInteracted?.Invoke(_isFault, facilityID)
      ├─ 회복 : FacilityManager가 정상 복귀 처리 + Clear()
      └─ 실패 : 상태 변경 알림만 (A의 버튼 클릭과 같다. 패널 다시 칠하기)
```

- 정상 상태에서 당기면 레버 애니메이션과 소리만 나고, 이벤트는 보내지 않는다. A, B는 정상 상태에서도 알림을 보내지만, C는 확률 판정이 없으므로 보낼 이유가 없다.
- 고장 난 순간 이미 레버가 내려가는 중이었다면(`IsPulling`), 그 당기기는 판정에 **포함한다.** 끝까지 내려간 시점에 고장이면 판정한다. 규칙을 단순하게 둔다.

### 프리팹 구성

```
FacilityC                ← FacilityC (facilityID 2, lever 연결)
└ Lever                  ← Lever.prefab 중첩 인스턴스 (+ AudioSource : 당기는 소리)
   ├ HandlePivot / Handle / Sphere   (Button 레이어, 콜라이더)
   └ Base
```

- `FacilityB.prefab`과 같은 방식으로 `Assets/Prefabs/FacilityC.prefab`을 만든다.
- 당기는 소리의 AudioSource는 `Lever.prefab` 쪽에 둔다 (모든 레버가 같은 소리). Play On Awake는 끈다.

## 6. 피드백

| 상황 | 시각 | 소리 | 담당 |
| --- | --- | --- | --- |
| 당김 (정상 / 고장 무관) | 레버가 내려갔다 올라온다 | '철컹' | Lever |
| 회복 성공 | 전면 패널 C 슬롯 초록 | 성공음 | MainPanelDisplay / (후속) FeedbackManager |
| 회복 실패 | 변화 없음 (C 슬롯이 계속 빨강) | 없음 (D4) | — |

- 실패는 "아무 변화 없음"으로 알 수 있어야 한다. 그래서 **레버를 전면 메인 패널이 보이는 곳에 두는 것**을 권장한다 (D7). 측면에 두면 결과를 보려고 고개를 돌려야 하고, 그때는 실패음이 필요해진다.

## 7. 매뉴얼 문구 (초안)

> C 요소가 고장 났을 시 레버를 당기시오. 한 번에 복구되지 않을 수 있음. 복구될 때까지 반복할 것.

- 확률 자체(25%)는 매뉴얼에 적지 않는다. 나폴리탄 괴담 톤으로 "당기는 횟수는 기록하지 말 것" 같은 문장을 붙여도 된다.

## 8. 수치

| 항목 | 초기값 | 위치 | 비고 |
| --- | --- | --- | --- |
| 회복 확률 (n) | 25 % | `FacilityC.repairChance` | 아래 표 참고 |
| 내려가는 / 올라오는 시간 | 0.25 초 | `Lever.pullDuration` | Animator 전환 시간과 같게 |
| 아래 머무는 시간 | 0.1 초 | `Lever.holdDuration` | |

회복까지 걸리는 시간 (계속 연타한다고 가정. k번째 판정 시각 = 0.25 + 0.6 × (k − 1) 초)

| 회복 확률 | 평균 당김 수 | 평균 시간 | 95%가 끝나는 당김 수 · 시간 |
| --- | --- | --- | --- |
| 15 % | 약 6.7회 | 약 3.7초 | 19회 · 약 11초 |
| **25 %** | **4회** | **약 2.1초** | **11회 · 약 6.3초** |
| 35 % | 약 2.9회 | 약 1.4초 | 7회 · 약 3.9초 |

- 제한 시간(40초) 대비 충분히 짧다. 단독 고장이면 쉬운 설비이고, A · B와 동시 고장일 때 "먼저 처리할 것"을 고르게 만드는 정도가 목표다.
- 한 번 당기는 시간이 짧아 연타하면 운의 비중이 줄어든다. 더 긴장감을 주려면 확률보다 **당기는 시간**(pullDuration, holdDuration)을 늘리는 쪽이 조작감에 맞다.

## 9. 결정 사항 (기본안)

| # | 항목 | 기본안 |
| --- | --- | --- |
| D1 | 당기기 방식 | **스프링 레버** : 클릭 한 번에 내려갔다 자동으로 올라온다 (3장 방안 A) |
| D2 | 판정 시점 | **끝까지 내려간 순간** (클릭 0.25초 뒤). 클릭 즉시 판정하면 레버가 움직이는 도중에 패널이 먼저 초록이 된다 |
| D3 | 회복 확률 | **25 %** (8장) |
| D4 | 실패 피드백 | **따로 두지 않는다.** 패널 C 슬롯이 계속 빨강인 것으로 안다 (D7과 함께) |
| D5 | 연속 실패 보정 | **넣지 않는다.** 25%면 11회 안에 95%가 끝나 시간 부담이 작다. 운이 너무 나쁘다는 피드백이 나오면 "N회째는 반드시 성공"을 추가한다 |
| D6 | 레버 코드 | **기존 `Lever.cs`를 고친다.** 클래스 · 프리팹을 새로 만들지 않는다 |
| D7 | 설비 C 위치 | [미정]. 권장 : **전면, 메인 패널 아래** (결과가 바로 보인다). 「게임 진행 규칙」 2장 공간 구조와 함께 정한다 |
| D8 | 고장 판정 | **고장 플래그**(`_isFault`). 장치에 맞출 목표 상태가 없으므로 A, B의 "장치 ≠ 목표" 방식을 쓰지 않는다 |

## 10. 작업 순서

1. `Lever.cs` 수정 : `Awake` 캐시, `OnPulled` 이벤트, Pull 코루틴(3장), 연타 방지, `ResetLever`, `OnDisable`, 당기는 소리
2. `LeverTestScene`에서 레버 단독 동작 확인 : 클릭 한 번에 내려갔다 올라오는지, 연타가 무시되는지
3. `FacilityC.cs` 작성 (5장)
4. `FacilityC.prefab` 구성 : FacilityC + Lever 중첩 인스턴스, 당기는 소리 AudioSource
5. `RefactoringTesetScene`에 배치 : `FacilityManager._facilities`의 **인덱스 2**에 등록, `facilityID = 2`
6. 테스트 : `FaultScheduler`가 C를 고르도록 여러 번 돌리거나, 임시로 인스펙터 · 테스트 코드에서 `MakeFault()` 호출
7. 11장 완료 기준 확인, 확률 · 시간 조정
8. 규칙 문서 갱신 (12장)

## 11. 완료 기준

| # | 확인 항목 |
| --- | --- |
| 1 | 조준선으로 레버 손잡이를 클릭하면 레버가 내려갔다가 스스로 올라온다. 한 번 클릭으로 끝난다 |
| 2 | 레버가 움직이는 동안 클릭해도 무시된다 (당기기가 겹치지 않는다) |
| 3 | 설비 C가 정상일 때 당기면 레버와 소리만 동작하고, 패널 · 타이머에 변화가 없다 |
| 4 | 설비 C가 고장일 때 당기면 레버가 끝까지 내려간 순간 판정되고, 성공하면 패널 C 슬롯이 초록으로 바뀐다 |
| 5 | `repairChance`를 1로 두면 한 번에, 0으로 두면 절대 회복되지 않는다 |
| 6 | 회복 직후 레버가 순간이동하지 않고 자연스럽게 올라온다 |
| 7 | C만 고장이었다면 회복 후 타이머가 멈추고 진행도가 다시 오른다 (FacilityManager 기존 흐름) |
| 8 | `LeverTestScene`의 레버도 오류 없이 내려갔다 올라온다 |
| 9 | 버튼(A), 슬라이더(B) 조작은 기존대로 동작한다 |

## 12. 규칙 문서 갱신

설비 C 규칙을 확정하면 아래를 고친다.

| 문서 | 위치 | 고칠 내용 |
| --- | --- | --- |
| 「게임 개요 및 핵심 경험」 | 3장 핵심 메카닉 · 설비 | `C : [미정]` → `C : 레버를 당겨 확률적으로 복구한다. 복구될 때까지 반복한다` |
| 「게임 진행 규칙」 | 2장 설비 배치 표 | C 행 : 위치(D7), 구성 "레버 1개" |
| 「게임 진행 규칙」 | 4장 고장 이벤트 표 | C가 들어갈 이벤트 추가 (예 : 3, 4번 이벤트를 "무작위 2개"로) |
| 「게임 진행 규칙」 | 7장 설비별 명세 | 설비 C 절 : 조작, 목표 생성(없음), 성공 판정(확률), 조작 피드백, 매뉴얼 문구 (5 ~ 7장) |
| 「게임 진행 규칙」 | 10장 수치 | 회복 확률 25%, 당기는 시간 |
| 「게임 진행 규칙」 | 남은 작업 | "설비 C [미정]" 행 제거, 새 기본안 추가 |
| 「시스템 명세_참고만」 | 2장 조작 장치, 3장 설비 | `LeverDevice`(= `Lever`), `FacilityC` 항목 추가 |

## 13. 영향받는 파일

| 파일 | 변경 |
| --- | --- |
| `Assets/Scripts/Lever/Lever.cs` | 4장 |
| `Assets/Scripts/Facility/FacilityC.cs` | 새 파일 (5장) |
| `Assets/Prefabs/Lever.prefab` | 당기는 소리 AudioSource 추가 |
| `Assets/Prefabs/FacilityC.prefab` | 새 프리팹 |
| `Assets/Scenes/RefactoringTesetScene.unity` | FacilityC 배치, `FacilityManager._facilities`에 등록 |
| `Assets/Sounds/` | 당기는 소리 에셋 (지금 없음) |
