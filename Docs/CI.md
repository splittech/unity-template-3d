# Сборка и релизы (GitHub Actions)

Workflow `.github/workflows/release.yml` собирает Windows-билд (StandaloneWindows64) с помощью Unity CLI (`unity`) и публикует его на странице **Releases** репозитория.

## Когда запускается

- **push в ветку `release`** (в том числе merge pull request);
- **pull request в ветку `release`** — при открытии, новых коммитах и переоткрытии. PR из форков пропускаются: им не доступны секреты.

Каждый запуск создаёт новый тег и релиз. К релизу прикладываются архив `unity-template-3d-vX.Y.Z-Windows64.zip` и `provenance.json` — манифест сборки (версия редактора, пакеты, коммит).

## Что делает workflow

1. Ставит Unity CLI (канал `beta`) на раннер `ubuntu-latest`.
2. Читает версию редактора из `ProjectSettings/ProjectVersion.txt` и ставит её командой `unity install` с модулем `windows-mono` (сборка Windows-плеера на Linux). Папка `Library` кэшируется.
3. Активирует лицензию самим редактором: для Personal — из файла `.ulf` (`-manualLicenseFile`), для Pro — по серийнику (`-serial -username -password`).
4. Собирает билд: `unity build --target StandaloneWindows64 --versioning-strategy custom --build-version X.Y.Z`.
5. Упаковывает билд в zip и публикует релиз. При падении прикладывает папку `Logs` как артефакт. Для Pro в конце возвращает лицензию (`-returnlicense`), даже если сборка упала.

Бэкенд скриптов — Mono. Если переключите проект на IL2CPP, Windows-билд на Linux-раннере собрать не получится — понадобится раннер `windows-latest`.

## Версия

- Берётся последний тег вида `vX.Y.Z`, патч увеличивается на 1: `v0.1.3` → `v0.1.4`.
- Если тегов ещё нет, первый релиз получает `bundleVersion` из Project Settings (сейчас `0.1.0`).
- Чтобы поднять мажорную или минорную версию, поставьте тег вручную, например `git tag v0.2.0 && git push origin v0.2.0`, — следующий релиз будет `v0.2.1`.
- Версия прописывается в билд (`Application.version`).

## Секреты

Добавьте в **Settings → Secrets and variables → Actions → New repository secret**:

| Секрет | Что положить |
|---|---|
| `UNITY_EMAIL` | Почта Unity-аккаунта |
| `UNITY_PASSWORD` | Пароль Unity-аккаунта |
| `UNITY_LICENSE` | Для **Personal**: всё содержимое файла лицензии `Unity_lic.ulf` |
| `UNITY_SERIAL` | Только для **Pro/Plus**: серийный номер (вместо `UNITY_LICENSE`) |

Unity Personal нельзя активировать из командной строки одним входом в аккаунт: на раннере нет Unity Hub и браузера, а в Unity CLI вход возможен только через браузер. Поэтому для Personal нужен файл лицензии `Unity_lic.ulf`.

Простого входа в Hub для этого мало: при входе Hub выдаёт лицензию-«entitlement» (`%LOCALAPPDATA%\Unity\licenses\UnityEntitlementLicense.xml`), она привязана к компьютеру и в CI не подходит. Файл `.ulf` появляется, если явно добавить лицензию: **Unity Hub → Settings (шестерёнка) → Licenses → Add license → Get a free personal license**. После этого откройте файл:

- Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
- macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
- Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

Скопируйте его содержимое целиком (это XML) в секрет `UNITY_LICENSE`. Personal-лицензия периодически перевыпускается; если сборка начнёт падать на активации, обновите секрет свежим файлом.
