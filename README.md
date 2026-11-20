# fastGPT

fastGPT добавляет команду **«Открыть с помощью Codex»** в контекстное меню Проводника Windows. Она запускает [Codex CLI](https://developers.openai.com/codex/cli) с выбранной папкой в качестве рабочей директории.

[English version](#english)

## Установка и запуск

1. Установите Codex CLI. Команда codex должна быть доступна в PATH.
2. Скачайте [fastGPT-Setup.exe](https://github.com/scalaphxr/fastGPT/releases/latest/download/fastGPT-Setup.exe), запустите его и нажмите **Установить**.
3. Щёлкните правой кнопкой мыши по папке, диску или свободному месту внутри папки. Выберите **«Открыть с помощью Codex»**. В Windows 11 этот пункт доступен в меню **«Показать дополнительные параметры»**.

Установка и запуск работают с правами текущего пользователя. При первом запуске Codex CLI может предложить войти в аккаунт.

Для удаления откройте **Параметры → Приложения → Установленные приложения → fastGPT → Удалить**.

## Сборка

~~~powershell
powershell -ExecutionPolicy Bypass -File build.ps1
~~~

На 64-разрядной Windows сборка использует компилятор .NET Framework 4 и создаёт dist\fastGPT-Setup.exe.

## English

fastGPT adds **Open with Codex** to the Windows File Explorer context menu. It launches [Codex CLI](https://developers.openai.com/codex/cli) with the selected folder as its working directory.

1. Install Codex CLI and make sure codex is on your PATH.
2. Download and run [fastGPT-Setup.exe](https://github.com/scalaphxr/fastGPT/releases/latest/download/fastGPT-Setup.exe) and select **Install**.
3. Right-click a folder, a drive, or empty space inside a folder. Choose **Open with Codex**. On Windows 11, look under **Show more options**.

Setup and launch use your existing user permissions. Codex CLI may prompt you to sign in on first launch.

To remove fastGPT, go to **Settings → Apps → Installed apps → fastGPT → Uninstall**. To build the installer on 64-bit Windows, run the PowerShell command above.

The context menu icon comes from the [ChatGPT website](https://chatgpt.com/). The icon belongs to OpenAI. fastGPT is an unofficial project.
