using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("fastGPT")]
[assembly: AssemblyProduct("fastGPT")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Один exe на всё: без аргументов это установщик, с "open <папка>" - запуск Codex.
static class FastGPT
{
    public const string Version = "1.0.0";
    const string MenuKey = "OpenWithCodex";
    const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\fastGPT";
    public const string CodexUrl = "https://developers.openai.com/codex/cli";

    // Directory - клик по папке, Directory\Background - по пустому месту внутри папки, Drive - по диску.
    static readonly string[] ShellClasses = { "Directory", @"Directory\Background", "Drive" };

    static readonly bool Russian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
    public static string T(string ru, string en) { return Russian ? ru : en; }

    static string ExePath { get { return Assembly.GetExecutingAssembly().Location; } }
    static string InstallDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\fastGPT"); } }
    static string InstalledExe { get { return Path.Combine(InstallDir, "fastGPT.exe"); } }

    [STAThread]
    static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "";
        bool silent = Array.IndexOf(args, "--silent") >= 0;

        if (mode == "open")
            return args.Length > 1 ? Open(args[1]) : 1;

        if (mode == "install" && silent)
        {
            Install();
            return 0;
        }

        if (mode == "uninstall")
        {
            if (!silent && !Confirm(T("Удалить fastGPT?", "Remove fastGPT?")))
                return 0;
            Uninstall();
            if (!silent)
                Info(T("fastGPT удалён.", "fastGPT has been removed."));
            return 0;
        }

        Application.EnableVisualStyles();
        Application.Run(new SetupForm());
        return 0;
    }

    static int Open(string rawPath)
    {
        string path = rawPath.Trim('"');
        if (path.Length == 2 && path[1] == ':')
            path += "\\";

        if (!Directory.Exists(path))
        {
            Error(T("Папка недоступна\n", "Folder is unavailable\n") + path);
            return 1;
        }

        string codex = FindCodex();
        if (codex == null)
        {
            Error(T("Установите Codex CLI и добавьте его в PATH.\n", "Install Codex CLI and add it to PATH.\n") + CodexUrl);
            return 1;
        }

        string system = Environment.SystemDirectory;
        var start = new ProcessStartInfo(Path.Combine(system, "cmd.exe"),
            "/d /v:off /k \"\"%FASTGPT_CODEX%\" --cd \"%FASTGPT_FOLDER%\"\"")
        {
            UseShellExecute = false,
            WorkingDirectory = system
        };
        start.EnvironmentVariables["FASTGPT_CODEX"] = codex;
        string fullPath = Path.GetFullPath(path);
        start.EnvironmentVariables["FASTGPT_FOLDER"] = fullPath.EndsWith("\\", StringComparison.Ordinal)
            ? fullPath + "."
            : fullPath;
        try
        {
            Process.Start(start);
            return 0;
        }
        catch (Win32Exception e)
        {
            Error(e.Message);
            return 1;
        }
    }

    public static void Install()
    {
        Directory.CreateDirectory(InstallDir);
        if (!string.Equals(Path.GetFullPath(ExePath), Path.GetFullPath(InstalledExe), StringComparison.OrdinalIgnoreCase))
            File.Copy(ExePath, InstalledExe, true);

        string icon = InstalledExe;
        foreach (string shellClass in ShellClasses)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + shellClass + @"\shell\" + MenuKey))
            using (RegistryKey command = key.CreateSubKey("command"))
            {
                key.SetValue("", T("Открыть с помощью Codex", "Open with Codex"));
                key.SetValue("Icon", icon);
                command.SetValue("", "\"" + InstalledExe + "\" open \"%V\\.\"");
            }
        }

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            key.SetValue("DisplayName", "fastGPT");
            key.SetValue("DisplayVersion", Version);
            key.SetValue("Publisher", "scalaphxr");
            key.SetValue("DisplayIcon", InstalledExe);
            key.SetValue("InstallLocation", InstallDir);
            key.SetValue("UninstallString", "\"" + InstalledExe + "\" uninstall");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

        }
    }

    public static void Uninstall()
    {
        foreach (string shellClass in ShellClasses)
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + shellClass + @"\shell\" + MenuKey, false);
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);

        // Windows unlocks the installed executable when this process exits.
        string system = Environment.SystemDirectory;
        var cleanup = new ProcessStartInfo(Path.Combine(system, "cmd.exe"),
            "/d /v:off /c ping -n 3 127.0.0.1 >nul & del /q \"%FASTGPT_INSTALLED_EXE%\" & rd \"%FASTGPT_INSTALL_DIR%\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = system
        };
        cleanup.EnvironmentVariables["FASTGPT_INSTALLED_EXE"] = InstalledExe;
        cleanup.EnvironmentVariables["FASTGPT_INSTALL_DIR"] = InstallDir;
        Process.Start(cleanup);
    }

    public static bool IsInstalled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallKey))
            return key != null;
    }

    public static bool IsCodexAvailable()
    {
        return FindCodex() != null;
    }

    public static bool IsWindows11()
    {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
        {
            int build;
            return key != null && int.TryParse(key.GetValue("CurrentBuild") as string, out build) && build >= 22000;
        }
    }

    static string FindCodex()
    {
        return FindOnPath("codex.exe") ?? FindOnPath("codex.cmd");
    }

    static string FindOnPath(string fileName)
    {
        string dirs = (Environment.GetEnvironmentVariable("PATH") ?? "") + ";" +
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local\bin");
        foreach (string dir in dirs.Split(';'))
        {
            try
            {
                string folder = dir.Trim().Trim('"');
                if (folder.Length >= 3 && Path.IsPathRooted(folder) &&
                    (folder[1] == ':' && folder[2] == '\\' || folder.StartsWith(@"\\", StringComparison.Ordinal)) &&
                    File.Exists(Path.Combine(folder, fileName)))
                    return Path.Combine(folder, fileName);
            }
            catch (ArgumentException) { } // в PATH бывают некорректные записи
        }
        return null;
    }

    public static void Info(string text) { MessageBox.Show(text, "fastGPT", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    public static void Error(string text) { MessageBox.Show(text, "fastGPT", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    public static bool Confirm(string text) { return MessageBox.Show(text, "fastGPT", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
}

class SetupForm : Form
{
    public SetupForm()
    {
        Text = "fastGPT " + FastGPT.Version;
        Font = SystemFonts.MessageBoxFont;
        Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        int textWidth;
        using (Graphics g = CreateGraphics())
            textWidth = (int)(440 * g.DpiX / 96);

        var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Padding = new Padding(16) };
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(textWidth, 0),
            Margin = new Padding(0, 0, 0, 12),
            Text = FastGPT.T(
                "Добавляет в меню правого клика по папке пункт «Открыть с помощью Codex». Он открывает cmd и запускает Codex CLI в этой папке.",
                "Adds \"Open with Codex\" to the right-click menu of folders. It opens a cmd window and starts Codex CLI in that folder.")
        });

        if (!FastGPT.IsCodexAvailable())
        {
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(textWidth, 0),
                Margin = new Padding(0, 0, 0, 12),
                ForeColor = Color.Firebrick,
                Text = FastGPT.T(
                    "Codex CLI не найден на этом компьютере. Установите его с " + FastGPT.CodexUrl + ", иначе пункт меню не сработает.",
                    "Codex CLI was not found on this computer. Install it from " + FastGPT.CodexUrl + ", otherwise the menu item will not work.")
            });
        }

        var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0), WrapContents = false };
        layout.Controls.Add(buttons);

        var install = new Button { AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8, 2, 8, 2), Text = FastGPT.IsInstalled() ? FastGPT.T("Переустановить", "Reinstall") : FastGPT.T("Установить", "Install") };
        install.Click += delegate { OnInstall(); };
        buttons.Controls.Add(install);
        AcceptButton = install;

        if (FastGPT.IsInstalled())
        {
            var uninstall = new Button { AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8, 2, 8, 2), Text = FastGPT.T("Удалить", "Remove") };
            uninstall.Click += delegate { OnUninstall(); };
            buttons.Controls.Add(uninstall);
        }

        var close = new Button { AutoSize = true, Margin = new Padding(0), Padding = new Padding(8, 2, 8, 2), Text = FastGPT.T("Закрыть", "Close") };
        close.Click += delegate { Close(); };
        buttons.Controls.Add(close);
        CancelButton = close;
    }

    void OnInstall()
    {
        try
        {
            FastGPT.Install();
        }
        catch (Exception e)
        {
            FastGPT.Error(e.Message);
            return;
        }

        string message = FastGPT.T("Готово! Нажмите правой кнопкой на любую папку и выберите «Открыть с помощью Codex».", "Done! Right-click any folder and choose \"Open with Codex\".");
        if (FastGPT.IsWindows11())
            message += FastGPT.T("\n\nВ Windows 11 пункт находится в «Показать дополнительные параметры».", "\n\nOn Windows 11 the item is under \"Show more options\".");
        FastGPT.Info(message);
        Close();
    }

    void OnUninstall()
    {
        if (!FastGPT.Confirm(FastGPT.T("Удалить fastGPT?", "Remove fastGPT?")))
            return;
        try
        {
            FastGPT.Uninstall();
        }
        catch (Exception e)
        {
            FastGPT.Error(e.Message);
            return;
        }
        FastGPT.Info(FastGPT.T("fastGPT удалён.", "fastGPT has been removed."));
        Close();
    }
}
