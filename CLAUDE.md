# Nature Bears Expedition — Project Rules

2D incremental idle-manager game. Casual mobile players, 5–10 minute sessions.

## Project Facts
- Unity **6000.5.3f1**, 2D **URP**, mobile **iOS + Android** (Android = IL2CPP).
- **New Input System only** (`activeInputHandler: 1`); actions asset at `Assets/Settings/InputSystem_Actions.inputactions`.
- .NET Standard 2.1. Newtonsoft JSON via `com.unity.nuget.newtonsoft-json`.
- All custom files live under `Assets/_Project/` (third-party plugins stay outside it).

## Architecture Rules (STRICT)
1. **SignalBus only** — systems never reference each other directly. Communication is exclusively via `NatureBears.Core.SignalBus`: static typed bus, plain-struct signals, `Subscribe<T>/Unsubscribe<T>/Fire<T>(in T)`. Define signals in `Scripts/Core/Signals.cs` (or feature-local signal files).
2. **Data-driven via ScriptableObjects** — all resources, buildings, biomes, bear stats, upgrade costs, and skill nodes are SO assets. Definitions in `Scripts/Data/`, instances in `Assets/_Project/ScriptableObjects/`. `[CreateAssetMenu(menuName = "NatureBears/...")]`.
3. **Economy numbers are `double`** everywhere (no BigDouble). Format for display with a shared NumberFormatter in `Scripts/Utils/` (1.2K / 3.4M / 5.6B …).
4. **Single asmdef**: `Assets/_Project/Scripts/NatureBears.asmdef` (root namespace `NatureBears`, sub-namespace per folder: `NatureBears.Core`, `.Data`, `.Gameplay`, `.UI`, `.Save`, `.Monetization`, `.Audio`, `.Utils`). Keep `overrideReferences: false` — Newtonsoft.Json.dll is auto-referenced only while it stays false.
5. **Domain-reload safety** — every static mutable field (bus registries, singleton `Instance`, session keys) must reset via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
6. **Allocation-conscious** — no per-frame LINQ, boxing, or string concat; mobile-first.
7. **Visible entities get an Animator**; managers must accept placeholder sprites/animations and allow hot-swapping final assets later.

## Save System
- AES-CBC (PKCS7, random IV prepended) encrypted Newtonsoft-JSON at `Application.persistentDataPath/naturebears.sav`. Key = PBKDF2(const secret + `SystemInfo.deviceUniqueIdentifier`), cached once per session.
- Hybrid cloud: `ICloudSaveProvider` interface (Null implementation until a backend is chosen). `INetworkTimeService` is the NTP stub hook.
- **Anti-time-skip**: on load, if `DateTime.UtcNow < lastSaveTimeUtc` → offline earnings capped to **0** for that session; on the FIRST offense fire `TimeCheatDetectedSignal` → one-time humorous achievement **"Güzel denemeydi!"** (gated by `timeCheatAchievementShown`).
- Runtime currency values wrapped in `ObscuredDouble`/`ObscuredLong` (`Scripts/Utils/ObscuredTypes.cs`); tamper → value 0 + `CurrencyTamperDetectedSignal`. SaveData persists plain doubles (file is already encrypted).
- `Assets/_Project/link.xml` preserves Newtonsoft.Json + SaveData from IL2CPP stripping — keep it updated when adding reflection-serialized types.

## UI / UX (Diegetic)
- Submenus (Build, Research, Manage) open as full-screen **"Scout Diaries"**.
- Shop is a **"Trading Post"** run by an NPC.
- Autumn/Sunset warm palette; hand-painted cozy aesthetic.

## Audio
- `AudioManager` drives a Unity **AudioMixer** with exposed params: `MasterVolume`, `BGMVolume`, `AmbientVolume`, `SFXVolume` (linear 0–1 → dB via `Log10(v)*20`).
- BGM: lo-fi acoustic guitar + soft marimba. Ambient: ASMR organic loops (fire crackle, stream). SFX: tactile (twig snaps, bubble pops for UI).
- The mixer asset (`Assets/_Project/Audio/MainMixer.mixer`) and its exposed params must be authored **in-editor**; AudioManager is null-guarded until then.

## Core Loop (for context)
Tap trees → Timberwood; tap Campfire → Fever Pitch gauge (30s x2 boost, daily-limited). Resources → Global Stash. Timberwood burns to Embers; Chef Panda crafts Meals from Salmon+Wildflowers+Mushrooms+Embers; Meals auto-sell for Golden Honey. Visible animated workers: Grizzly Lumberjack, Chef Panda. Math-only passives: Polar Angler, Sun Bear Forager, Black Bear Scout. Prestige = **Hibernation** (keep Slumber Points + permanent SlumberSkillNode tree: Offline/Active/Production branches); biome progression via `BiomeData` (Whispering Pines, Frostpeak Lake, Cherry Blossom Valley, Amberwood Grove, Starlit Tundra).

## Monetization (dummy managers, no SDKs yet)
- `AdManager`: rewarded types **EagleDrop** (eagle drops a physical box in camp) and **OfflineMultiplier2x**; interstitials disabled once RemoveAds owned.
- `IAPManager`: `com.naturebears.removeads` permanent purchase; restore flow stubbed.

## Workflow
- **Ask before assuming** — if a requirement/architecture choice is ambiguous, stop and ask the user. Present plans before large code batches.
- After file changes, let Unity refresh/compile and check `mcp__unity-mcp__Unity_GetConsoleLogs` for errors before declaring done.
