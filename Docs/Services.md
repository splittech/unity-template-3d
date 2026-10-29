# Сервисы проекта

Документ описывает сервисы из `Assets/Game/Scripts/Services`: что каждый делает, его интерфейс и как пользоваться им из своего кода.

**Сервис** — это класс, который отвечает за одну общую задачу всей игры (загрузка сцен, звук, логирование) и существует в одном экземпляре. Свои классы не создают сервисы сами и не ищут их через `FindObjectOfType`, а получают готовые через **внедрение зависимостей** (Dependency Injection, DI) — см. раздел «Как получить сервис в своём классе».

## Содержание

1. [Сторонние библиотеки](#сторонние-библиотеки)
2. [Как получить сервис в своём классе (VContainer)](#как-получить-сервис-в-своём-классе-vcontainer)
3. [Асинхронные методы (UniTask)](#асинхронные-методы-unitask)
4. [Подписки на события (R3)](#подписки-на-события-r3)
5. [Logging Service — логирование](#logging-service--логирование)
6. [Scene Service — загрузка сцен](#scene-service--загрузка-сцен)
7. [Loading Screen — экран загрузки](#loading-screen--экран-загрузки)
8. [Audio Service — музыка и звуки](#audio-service--музыка-и-звуки)
9. [Application Service — выход из игры](#application-service--выход-из-игры)
10. [Инструменты редактора](#инструменты-редактора)

---

## Сторонние библиотеки

| Библиотека | Зачем нужна в проекте |
|---|---|
| [VContainer](https://vcontainer.hadashikick.jp/) | DI-контейнер: создаёт сервисы и передаёт их в ваши классы. |
| [UniTask](https://github.com/Cysharp/UniTask) | Асинхронные методы (`async`/`await`) для Unity. Все долгие операции сервисов (загрузка сцен, показ экрана загрузки) возвращают `UniTask`. |
| [R3](https://github.com/Cysharp/R3) | Реактивные подписки на события, например на нажатие кнопки. |
| [DOTween](https://dotween.demigiant.com/) | Анимации (плавное появление экрана загрузки, затухание музыки). Используется внутри сервисов, напрямую работать с ним не нужно. |
| [Alchemy](https://github.com/annulusgames/Alchemy) | Улучшенный инспектор: группы полей, словари, кнопки в конфигах. |
| [Eflatun.SceneReference](https://github.com/starikcetin/Eflatun.SceneReference) | Поле для выбора сцены в инспекторе (вместо ввода имени сцены строкой). |

---

## Как получить сервис в своём классе (VContainer)

### Scope'ы

Сервисы регистрируются в **LifetimeScope** — компоненте на сцене, который описывает, какие объекты существуют и как их создавать.

| Scope | Сцена | Что в нём |
|---|---|---|
| `ProjectScope` | Boot Scene | Сервисы на всю игру: `ISceneService`, `ILoggingService`, `IGameLoadingScreen`. |
| `MainMenuScope` | Main Menu Scene | Объекты главного меню. Родитель — `ProjectScope`. |
| `GameplayScope` | Gameplay Scene | Объекты геймплея. Родитель — `ProjectScope`. |

Boot Scene загружается первой и не выгружается, поэтому сервисы из `ProjectScope` живут всё время работы игры. Дочерние scope'ы (`MainMenuScope`, `GameplayScope`) видят всё, что зарегистрировано в родителе, поэтому в объектах меню и геймплея можно получать любые сервисы проекта.

Регистрация выглядит так (`Assets/Game/Scripts/App/ProjectScope.cs`):

```csharp
protected override void Configure(IContainerBuilder builder)
{
    // Обычный C#-класс: контейнер создаст один экземпляр (Singleton)
    // и будет отдавать его всем, кто запросит ISceneService.
    builder.Register<SceneService>(Lifetime.Singleton).As<ISceneService>();

    // Уже существующий компонент со сцены.
    builder.RegisterComponent(_loadingScreen).As<IGameLoadingScreen>();

    // Готовый объект (например, ScriptableObject-конфиг).
    builder.RegisterInstance(_sceneServiceConfig);
}
```

> Запрашивайте сервисы **по интерфейсу** (`ISceneService`), а не по классу (`SceneService`). Регистрация `.As<ISceneService>()` означает, что контейнер выдаёт объект именно по интерфейсу.

### Обычный C#-класс — через конструктор

Перечислите нужные сервисы в параметрах конструктора и зарегистрируйте класс в своём scope'е. Создавать объект через `new` не нужно — это сделает контейнер.

```csharp
using Game.Services.LoggingService;
using Game.Services.SceneService;

public sealed class LevelController
{
    private readonly ISceneService _sceneService;
    private readonly IGameLogger _logger;

    public LevelController(ISceneService sceneService, ILoggingService loggingService)
    {
        _sceneService = sceneService;
        _logger = loggingService.GetLogger(LoggingChannel.SceneService);
    }
}
```

```csharp
// GameplayScope.cs
protected override void Configure(IContainerBuilder builder)
{
    builder.Register<LevelController>(Lifetime.Singleton);
}
```

### MonoBehaviour — через метод с `[Inject]`

У MonoBehaviour нельзя использовать конструктор (объект создаёт Unity), поэтому зависимости приходят в метод с атрибутом `[Inject]`. Название метода может быть любым, в проекте принято `Construct`.

```csharp
using Game.Services.SceneService;
using UnityEngine;
using VContainer;

public class MyMenuWindow : MonoBehaviour
{
    private ISceneService _sceneService;

    [Inject]
    public void Construct(ISceneService sceneService)
    {
        _sceneService = sceneService;
    }

    private void Start()
    {
        // Здесь _sceneService уже заполнен.
    }
}
```

Чтобы контейнер вызвал `Construct`, он должен знать об объекте. Есть три способа:

1. **Объект уже лежит на сцене** — добавьте его GameObject в список **Auto Inject Game Objects** в инспекторе LifetimeScope этой сцены. Зависимости получат все компоненты этого GameObject и его дочерних объектов. Так подключён `SwitchSceneButton` в Main Menu Scene.
2. **Объект нужно зарегистрировать как зависимость** для других классов — `builder.RegisterComponent(component)` в `Configure`.
3. **Объект создаётся из префаба во время игры** — используйте `IObjectResolver.Instantiate` вместо обычного `Object.Instantiate`, иначе `[Inject]` не сработает:

```csharp
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy _enemyPrefab;

    private IObjectResolver _resolver;

    [Inject]
    public void Construct(IObjectResolver resolver)
    {
        _resolver = resolver;
        // Альтернатива: прокинуть все зависимости в спавнер и при создании врага явно предать их. Это предпочтительный вариант так как использование IObjectResolver нежелательно с точки зрения архитектуры.
    } 

    public Enemy Spawn(Vector3 position)
    {
        return _resolver.Instantiate(_enemyPrefab, position, Quaternion.identity);
    }
}
```

> Не обращайтесь к сервисам в `Awake`: порядок `Awake` разных объектов не гарантирован, и внедрение может ещё не произойти. Используйте `Start` и позже.

---

## Асинхронные методы (UniTask)

Загрузка сцены или анимация экрана занимают несколько кадров. Такие методы сервисов **асинхронные**: имя заканчивается на `Async`, а тип результата — `UniTask`. Это аналог корутин, но удобнее: результат можно дождаться через `await` прямо в коде.

### Как вызывать

```csharp
using Cysharp.Threading.Tasks;

// 1. Метод, который ждёт другие асинхронные методы, помечается async и возвращает UniTask.
private async UniTask GoToGameplayAsync()
{
    await _loadingScreen.ShowScreenAsync();                  // ждём, пока экран появится
    await _sceneService.SwitchSceneAsync(_gameplayScene);    // ждём загрузки сцены
    await _loadingScreen.HideScreenAsync();                  // ждём, пока экран скроется
}

// 2. Из обычного (не async) метода — Start, обработчика кнопки — запуск через .Forget().
private void Start()
{
    GoToGameplayAsync().Forget();
}
```

- `await` — «подожди завершения и продолжи со следующей строки». Игра при этом не зависает: остальной код и кадры продолжают выполняться.
- `.Forget()` — «запусти и не жди». Используйте его, когда вызываете асинхронный метод из обычного. Без `.Forget()` компилятор выдаст предупреждение, а ошибки внутри метода могут потеряться.
- Не используйте `async void` — вместо него `async UniTask` + `.Forget()`.

### Отмена (`CancellationToken ct`)

Почти все асинхронные методы принимают необязательный параметр `CancellationToken ct`. Он позволяет прервать операцию, например если объект, который её запустил, уничтожили. Если не передать — операция выполнится до конца.

В MonoBehaviour есть готовый токен `destroyCancellationToken`, который срабатывает при уничтожении объекта:

```csharp
private void Start()
{
    GoToGameplayAsync(destroyCancellationToken).Forget();
}

private async UniTask GoToGameplayAsync(CancellationToken ct)
{
    await _loadingScreen.ShowScreenAsync(ct);
    await _sceneService.SwitchSceneAsync(_gameplayScene, ct: ct);
}
```

При отмене `await` выбрасывает `OperationCanceledException` — это нормально, `.Forget()` не выводит её как ошибку.

### Прогресс (`IProgress<float> progress`)

Методы загрузки принимают необязательный параметр `progress` — объект, в который сервис сообщает прогресс от `0` до `1`. Чаще всего туда передают `_loadingScreen.LoadingProgress`, и полоска экрана загрузки заполняется сама. Свой обработчик создаётся так:

```csharp
var progress = Progress.Create<float>(value => Debug.Log($"Загружено {value:P0}"));
await _sceneService.LoadSceneAsync(_scene, progress);
```

Чтобы объединить прогресс нескольких операций в одну полоску, используйте `CompositeProgress` (`Game.Services.Common`):

```csharp
var total = new CompositeProgress(value => _loadingScreen.LoadingProgress.Report(value));
IProgress<float> scenes = total.CreateSubProgress(weight: 3); // загрузка сцен — 75% полоски
IProgress<float> data   = total.CreateSubProgress(weight: 1); // загрузка данных — 25%

await _sceneService.LoadSceneAsync(_scene, scenes);
await LoadSaveDataAsync(data);
```

---

## Подписки на события (R3)

R3 позволяет подписаться на событие (нажатие кнопки, изменение значения) и автоматически отписаться, когда объект уничтожен. В проекте он используется для UI-кнопок:

```csharp
using R3;

private void Start()
{
    _button.OnClickAsObservable()                       // поток событий «кнопку нажали»
        .Subscribe(_ => SwitchSceneAsync().Forget())    // что делать при каждом нажатии
        .AddTo(this);                                   // отписаться при уничтожении этого объекта
}
```

- `OnClickAsObservable()` — превращает `Button.onClick` в поток событий.
- `Subscribe(...)` — код, который выполнится при каждом событии. Параметр `_` — само событие (для кнопки оно пустое).
- `.AddTo(this)` — **обязательно**: без него подписка продолжит жить после уничтожения объекта.
- Асинхронный метод внутри `Subscribe` запускается через `.Forget()` (см. UniTask).

---

## Logging Service — логирование

**Namespace:** `Game.Services.LoggingService`
**Зарегистрирован в:** `ProjectScope` как `ILoggingService`
**Конфиг:** `LoggingServiceConfig` (*Create → Game → Configs → Logging Service Config*)

Обёртка над `Debug.Log`. Логи делятся на **каналы** (`LoggingChannel`) — по одному на систему. Для каждого канала в конфиге настраивается префикс, его цвет в консоли, куда писать лог и не заглушён ли он. Так можно отключить «шумные» логи одной системы, не трогая код.

### Интерфейс

```csharp
public interface ILoggingService
{
    IGameLogger GetLogger(LoggingChannel channel);
}

public interface IGameLogger
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
}
```

| Член | Описание |
|---|---|
| `GetLogger(channel)` | Возвращает логгер для канала. Если канал не настроен в конфиге, вернётся «пустой» логгер, который ничего не выводит (ошибки не будет). |
| `Log` / `LogWarning` / `LogError` | Обычное сообщение, предупреждение, ошибка. В консоли Unity выглядят как `[Префикс] сообщение`. |

### Настройка канала (`LogChannelConfig`)

| Поле | Описание |
|---|---|
| `Muted` | Заглушить канал — сообщения не выводятся. |
| `Channel Prefix` | Текст в квадратных скобках перед сообщением. |
| `Prefix Color` | Цвет префикса в консоли. |
| `Logger Types` | Куда писать: `UnityLogger` (консоль Unity), `FileLogger`, `ServerLogger`. Можно выбрать несколько. **`FileLogger` и `ServerLogger` пока не реализованы** и ничего не делают. |

### Использование

Получите логгер один раз — в конструкторе или `Construct` — и храните в поле:

```csharp
private IGameLogger _logger;

[Inject]
public void Construct(ILoggingService loggingService)
{
    _logger = loggingService.GetLogger(LoggingChannel.Input);
}

private void OnJump() => _logger.Log("Jump pressed.");
```

Чтобы добавить свой канал: допишите значение в enum `LoggingChannel` (`Logging Service/Configs/LoggingChannel.cs`) и добавьте для него запись в `LoggingServiceConfig`.

---

## Scene Service — загрузка сцен

**Namespace:** `Game.Services.SceneService`
**Зарегистрирован в:** `ProjectScope` как `ISceneService`
**Конфиг:** `SceneServiceConfig` (*Create → Game → Configs → Scene Service Config*)

Загружает и выгружает сцены. Все сцены грузятся **аддитивно** — поверх уже открытых, а не вместо них. Поэтому Boot Scene с сервисами остаётся загруженной всегда, а меню и геймплей подгружаются и выгружаются рядом с ней.

### SceneMetaAsset

Сервис работает не с именами сцен, а с ассетами `SceneMetaAsset` (*Create → Game → Meta Assets → Scene Meta Asset*) — это «карточка» сцены:

| Поле | Описание |
|---|---|
| `Scene Reference` | Сама сцена (выбирается в инспекторе). Сцена должна быть добавлена в Build Settings. |
| `Initial` | Загружать при старте игры. |
| `Persistent` | Не выгружать при переключении сцен (`SwitchSceneAsync`). Подходит, например, для сцены с UI, который нужен всегда. |

В `SceneServiceConfig` указываются:
- `Core Scene` — Boot Scene (сцена с `ProjectScope`). Считается загруженной всегда.
- `Other Scenes` — все остальные сцены игры. Сцены, которой нет в этом списке, сервис не выгрузит при переключении.

Кнопка **Validate Scenes** в инспекторе конфига проверяет, что нет пустых ссылок и дубликатов.

### Интерфейс

У всех асинхронных методов есть необязательные параметры `progress` и `ct` (см. [UniTask](#асинхронные-методы-unitask)).

| Член | Описание |
|---|---|
| `HashSet<SceneMetaAsset> LoadedScenes` | Сцены, загруженные (или загружающиеся) сейчас. Только для чтения — не изменяйте коллекцию. |
| `UniTask LoadSceneAsync(scene)` | Загрузить сцену. Если она уже загружена — предупреждение в лог, ничего не произойдёт. |
| `UniTask UnloadSceneAsync(scene)` | Выгрузить сцену. Если она не загружена — предупреждение в лог. |
| `UniTask LoadSceneListAsync(scenes)` | Загрузить несколько сцен параллельно. Уже загруженные пропускаются. |
| `UniTask UnloadSceneListAsync(scenes)` | Выгрузить несколько сцен параллельно. |
| `UniTask SwitchSceneAsync(scene)` | **Основной способ сменить экран игры.** Выгружает все сцены, кроме `Persistent` и Core Scene, затем загружает указанную. |
| `UniTask LoadInitialScenesAsync()` | Загрузить сцены с флагом `Initial`. Вызывается один раз при старте в `ProjectBootstrap`. |
| `UniTask UnloadNonpersistentScenesAsync()` | Выгрузить все сцены без флага `Persistent`. |

### Использование

```csharp
public class GameOverWindow : MonoBehaviour
{
    [SerializeField] private SceneMetaAsset _mainMenuScene;   // перетащите ассет в инспекторе

    private ISceneService _sceneService;
    private IGameLoadingScreen _loadingScreen;

    [Inject]
    public void Construct(ISceneService sceneService, IGameLoadingScreen loadingScreen)
    {
        _sceneService = sceneService;
        _loadingScreen = loadingScreen;
    }

    public void OnBackToMenuClicked() => BackToMenuAsync().Forget();

    private async UniTask BackToMenuAsync()
    {
        await _loadingScreen.ShowScreenAsync();
        await _sceneService.SwitchSceneAsync(_mainMenuScene, _loadingScreen.LoadingProgress);
        await _loadingScreen.HideScreenAsync();
    }
}
```

> Если объект, запустивший `SwitchSceneAsync`, находится в выгружаемой сцене, не передавайте в метод его `destroyCancellationToken`: объект уничтожится посреди переключения, и операция отменится.

---

## Loading Screen — экран загрузки

**Namespace:** `Game.Services.LoadingScreen`
**Зарегистрирован в:** `ProjectScope` как `IGameLoadingScreen` (объект лежит в Boot Scene)

Экран, который закрывает игру, пока грузятся сцены, и показывает полоску прогресса и текст. Экран сам следит за тем, чтобы:
- он был виден не меньше `Minimum Show Time` секунд (чтобы не «мигал» при быстрой загрузке);
- полоска дошла до 100% перед скрытием, даже если загрузка не сообщила `1`;
- повторный вызов `Show` / `Hide` во время анимации корректно прерывал предыдущий.

Анимации работают и при `Time.timeScale = 0` (на паузе).

### Интерфейс

| Член | Описание |
|---|---|
| `IProgress<float> LoadingProgress` | Передавайте его в параметр `progress` методов Scene Service — полоска будет заполняться автоматически. |
| `UniTask ShowScreenAsync(ct)` | Плавно показать экран. Сбрасывает прогресс в 0. |
| `UniTask HideScreenAsync(ct)` | Дождаться 100% и минимального времени показа, затем плавно скрыть экран. |
| `void ShowScreenImmediate()` | Показать мгновенно, без анимации (используется при старте игры). |
| `void HideScreenImmediate()` | Скрыть мгновенно. |
| `void SetProgressDescription(string)` | Текст под полоской, например «Загрузка уровня». Очищается при скрытии. |

### Использование

Типичная последовательность — «показать → загрузить с прогрессом → скрыть»:

```csharp
await _loadingScreen.ShowScreenAsync();
_loadingScreen.SetProgressDescription("Загрузка уровня");
await _sceneService.SwitchSceneAsync(_levelScene, _loadingScreen.LoadingProgress);
await _loadingScreen.HideScreenAsync();
```

Обязательно ставьте `await` перед `ShowScreenAsync`: иначе загрузка начнётся до того, как экран закроет игру.

### Свой вариант экрана

Готовая реализация — `SimpleLoadingScreen` (Slider + TextMeshPro + CanvasGroup, появление через затухание). Чтобы сделать экран с другой анимацией, унаследуйтесь от `GameLoadingScreen` и реализуйте пять методов:

| Метод | Что сделать |
|---|---|
| `ShowAsync(ct)` / `HideAsync(ct)` | Анимация появления / исчезновения. Должна завершиться, когда анимация закончилась. |
| `SetVisibleImmediate(bool)` | Мгновенно показать или скрыть, остановив анимацию. |
| `UpdateProgressVisuals(float)` | Отрисовать прогресс от 0 до 1. Вызывается каждый кадр. |
| `SetProgressDescription(string)` | Вывести текст. |

Затем укажите новый компонент в поле `Loading Screen` у `ProjectScope` в Boot Scene.

---

## Audio Service — музыка и звуки

**Namespace:** `Game.Services.AudioService`
**Конфиг:** `AudioServiceConfig` (*Create → Game → Configs → Audio Service Config*)

> **Внимание:** сервис пока **не зарегистрирован** в `ProjectScope`, поэтому получить `IAudioService` через `[Inject]` сейчас нельзя. Чтобы подключить его, добавьте в `ProjectScope` поля для `MusicPlayer`, `SoundPlayer` и `AudioServiceConfig` (компоненты лежат в Boot Scene) и зарегистрируйте их:
>
> ```csharp
> builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
> builder.RegisterComponent(_musicPlayer);
> builder.RegisterComponent(_soundPlayer);
> builder.RegisterInstance(_audioServiceConfig);
> ```
>
> При старте игры вызовите `ApplyAllMixerGroupVolumes()`, чтобы применить сохранённую громкость.

Сервис проигрывает фоновую музыку и звуковые эффекты и управляет громкостью групп **Audio Mixer** (микшер Unity, где звук разделён на группы вроде Music, SFX, UI).

### Ассеты звуков

Звуки и музыка задаются ассетами, а не напрямую через `AudioClip`:

**`MusicMetaAsset`** (*Create → Game → Meta Assets → Music Meta Asset*): `Audio Clip`, `Volume`.

**`SoundMetaAsset`** (*Create → Game → Meta Assets → Sound Meta Asset*):

| Поле | Описание |
|---|---|
| `Audio Clip`, `Volume` | Звук и его громкость. |
| `Mixer Group` | Группа микшера. Для неё должен существовать пул в `SoundPlayer`, иначе звук не проиграется (ошибка в логе). |
| `Is Looping` | Зацикленный звук (шаги, двигатель). |
| `Random Pitch`, `Random Pitch Range` | Случайная высота тона при каждом проигрывании, чтобы одинаковые звуки не надоедали. |
| `Priority` | Если пул заполнен, новый звук вытеснит старый незацикленный звук с приоритетом не выше своего. |
| `Max Instances` | Сколько копий этого звука может звучать одновременно (0 — без ограничения). Лишняя копия вытесняет самую старую. |
| `Max Hear Distance` | Дальность слышимости для пространственного звука. |

### Интерфейс

| Член | Описание |
|---|---|
| `void PlayMusic(MusicMetaAsset)` | Включить музыку. Предыдущий трек плавно затухает (время — `Music Fade Time` в конфиге). |
| `void StopMusic()` | Плавно остановить музыку. |
| `SoundInstance PlaySound(sound, autoStart = true)` | Проиграть обычный (не пространственный) звук — для UI, интерфейса. |
| `SoundInstance PlaySpatialSound(sound, position, autoStart = true)` | Проиграть звук в точке мира: он тише издалека и слышен с нужной стороны. |
| `void SetMixerGroupVolume(group, volume)` | Задать громкость группы от 0 до 1. Значение сохраняется между запусками игры. |
| `float GetMixerGroupVolume(group)` | Текущая громкость группы (0–1). Удобно для начального положения слайдера в настройках. |
| `void ApplyAllMixerGroupVolumes()` | Применить сохранённую громкость ко всем группам. |

Методы `PlaySound` / `PlaySpatialSound` возвращают `null`, если звук проиграть не удалось (пул заполнен звуками с более высоким приоритетом или нет пула для группы) — проверяйте результат, если сохраняете его.

### SoundInstance

Объект проигрываемого звука. Для коротких звуков его можно не сохранять: после окончания звук освобождается автоматически.

| Член | Описание |
|---|---|
| `State` | `Pending` (создан, не запущен), `Playing`, `Paused`, `Stopped`, `Released` (освобождён). |
| `Play()`, `Stop()`, `Pause()`, `Unpause()` | Управление воспроизведением. |
| `SetPosition(Vector3)` | Переместить пространственный звук (например, следом за объектом). |
| `Release()` | Вернуть звук в пул досрочно. |

После `Stop()` или окончания звука экземпляр переходит в `Released` и больше не может использоваться — вызов `Play()` или `SetPosition()` выбросит исключение. Перед повторным использованием проверяйте `State`, а для нового проигрывания вызывайте `PlaySound` заново.

### Использование

```csharp
public class Door : MonoBehaviour
{
    [SerializeField] private SoundMetaAsset _openSound;
    [SerializeField] private SoundMetaAsset _alarmLoop;

    private IAudioService _audioService;
    private SoundInstance _alarm;

    [Inject]
    public void Construct(IAudioService audioService) => _audioService = audioService;

    public void Open()
    {
        // Короткий звук: запустили и забыли.
        _audioService.PlaySpatialSound(_openSound, transform.position);
    }

    public void StartAlarm() => _alarm = _audioService.PlaySpatialSound(_alarmLoop, transform.position);

    public void StopAlarm()
    {
        if (_alarm != null && _alarm.State != SoundState.Released)
            _alarm.Stop();

        _alarm = null;
    }
}
```

Слайдер громкости в настройках:

```csharp
[SerializeField] private Slider _musicSlider;
[SerializeField] private AudioMixerGroup _musicGroup;

private void Start()
{
    _musicSlider.value = _audioService.GetMixerGroupVolume(_musicGroup);
    _musicSlider.onValueChanged.AddListener(v => _audioService.SetMixerGroupVolume(_musicGroup, v));
}
```

### Настройка

- **`AudioServiceConfig`**: `Mixer` — ваш Audio Mixer; `Mixer Groups Configs` — для каждой группы имя **exposed-параметра** громкости и громкость по умолчанию; `Music Fade Time`.
  Exposed-параметр создаётся в окне Audio Mixer: выберите группу → в инспекторе ПКМ по `Volume` → *Expose 'Volume' to script*, затем переименуйте его в списке *Exposed Parameters* (правый верхний угол окна микшера). Если имя в конфиге не совпадает, при изменении громкости будет исключение.
- **`SoundPlayer`** (компонент в Boot Scene): `Sound Pools` — для каждой группы микшера число одновременно звучащих звуков.
- **`MusicPlayer`** (компонент в Boot Scene): `Music Mixer Group` — группа для музыки.

---

## Application Service — выход из игры

**Namespace:** `Game.Services.ApplicationService`

Один метод `QuitGame()`: закрывает приложение в билде и останавливает Play Mode в редакторе. Интерфейса нет, класс получают напрямую — `ApplicationService`.

> Сервис пока **не зарегистрирован**. Добавьте в `ProjectScope`:
> `builder.Register<ApplicationService>(Lifetime.Singleton);`

```csharp
[Inject]
public void Construct(ApplicationService applicationService) => _applicationService = applicationService;

public void OnQuitClicked() => _applicationService.QuitGame();
```

---

## Инструменты редактора

Пункты в меню **Game** верхней панели Unity:

| Пункт | Что делает |
|---|---|
| **Game → Play Mode → Start From Boot Scene** | Если включено (по умолчанию), Play Mode всегда стартует с Boot Scene, какая бы сцена ни была открыта. Без этого сервисы не будут созданы и `[Inject]` не сработает. После выхода из Play Mode открытые сцены восстанавливаются. |
| **Game → Config Manager** | Список всех конфигов проекта (вкладки Game и Services): найти существующие и создать новые. |
| **Game → Meta Asset Manager** | То же для мета-ассетов (`SceneMetaAsset`, `SoundMetaAsset`, `MusicMetaAsset`) с папкой по умолчанию для каждого типа. |

Свои конфиги и мета-ассеты попадают в эти окна автоматически, если у класса есть `[CreateAssetMenu]` с `menuName`, начинающимся на `Game/Configs/` или `Game/Meta Assets/`, и он лежит в сборке с именем `Game.*`.
