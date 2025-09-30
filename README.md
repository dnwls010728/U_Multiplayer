# U_Multiplayer

## Project Structure

```
Assets/                  # 모든 게임 에셋 (스크립트, 아트, 사운드 등)
├── App/                 # 핵심 애플리케이션 로직 및 데이터
│   ├── Animations/      # 애니메이션 클립 및 컨트롤러
│   ├── Art/             # 3D 모델, 텍스처, 머티리얼
│   │   ├── Materials/
│   │   ├── Models/
│   │   ├── Shaders/
│   │   └── Textures/
│   ├── Audio/           # 사운드 이펙트 및 배경 음악
│   │   ├── Music/
│   │   └── SFX/
│   ├── Prefabs/         # 재사용 가능한 게임 오브젝트
│   ├── Scenes/          # 게임 레벨, 메뉴 등 씬 파일
│   ├── ScriptableObjects/ # 데이터 저장을 위한 에셋
│   ├── Scripts/         # C# 스크립트
│   │   ├── Character/
│   │   ├── Core/
│   │   ├── Gameplay/
│   │   ├── Networking/
│   │   ├── ScriptableObjects/
│   │   └── UI/
│   └── UI/              # UI 요소 (아이콘, 폰트, UXML)
├── Editor/              # Unity 에디터 확장 스크립트
├── Multiplayer Widgets/ # 멀티플레이어용 UI 위젯
├── Plugins/             # 서드파티 플러그인 (DOTween 등)
├── Resources/           # Resources.Load()로 로드할 에셋
├── SceneTemplateAssets/ # 씬 템플릿용 에셋
├── Settings/            # 렌더링, 빌드 등 프로젝트 설정 에셋
├── StreamingAssets/     # 원본 파일 그대로 빌드에 포함될 파일
├── TextMesh Pro/        # TextMesh Pro 관련 에셋
├── Thirdparty/          # 기타 서드파티 라이브러리
└── UI Toolkit/          # UI Toolkit 관련 에셋
```
