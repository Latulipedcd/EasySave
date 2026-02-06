using System.Collections.Generic;
using System.Threading;
using EasySave.Application.ViewModels;

namespace EasySave.ConsoleApp
{
    public class ConsoleView
    {
        private readonly MainViewModel _vm;
        private int _mainMenuIndex;
        private bool _hasDisplayedIntro;

        private static readonly MenuEntry[] MenuEntries =
        [
            new("MenuOptionList", MainMenuAction.ListJobs),
            new("MenuOptionCreate", MainMenuAction.CreateJob),
            new("MenuOptionExecute", MainMenuAction.ExecuteJobs),
            new("MenuOptionModify", MainMenuAction.ModifyJob),
            new("MenuOptionDelete", MainMenuAction.DeleteJob),
            new("MenuOptionLang", MainMenuAction.ChangeLanguage),
            new("MenuOptionExit", MainMenuAction.Exit)
        ];

        public ConsoleView()
        {
            _vm = new MainViewModel();
        }

        public void Start()
        {
            InitializeLanguage();
            DisplayIntro();

            bool running = true;
            while (running)
            {
                MainMenuAction action = ShowMainMenu();
                switch (action)
                {
                    case MainMenuAction.ListJobs:
                        ShowActionScreen("MenuOptionList");
                        break;
                    case MainMenuAction.CreateJob:
                        ShowActionScreen("MenuOptionCreate");
                        break;
                    case MainMenuAction.ExecuteJobs:
                        ShowActionScreen("MenuOptionExecute");
                        break;
                    case MainMenuAction.ModifyJob:
                        ShowActionScreen("MenuOptionModify");
                        break;
                    case MainMenuAction.DeleteJob:
                        ShowActionScreen("MenuOptionDelete");
                        break;
                    case MainMenuAction.ChangeLanguage:
                        ChangeLanguage();
                        break;
                    case MainMenuAction.Exit:
                        running = false;
                        break;
                }
            }

            Console.ResetColor();
            TrySetCursorVisible(true);
            Console.Clear();
        }

        private void InitializeLanguage()
        {
            bool hasLoadedSavedLanguage = _vm.TryLoadSavedLanguage();
            if (!hasLoadedSavedLanguage)
            {
                RequestLanguageAtStartup();
            }
        }

        private void DisplayIntro()
        {
            if (_hasDisplayedIntro)
            {
                return;
            }

            _hasDisplayedIntro = true;
            TrySetCursorVisible(false);
            Console.Clear();

            string[] logo =
            [
                "   ______                 _____                 ",
                "  |  ____|               / ____|                ",
                "  | |__   __ _ ___ _   _| (___   __ ___   _____ ",
                "  |  __| / _` / __| | | |\\___ \\ / _` \\ \\ / / _ \\",
                "  | |___| (_| \\__ \\ |_| |____) | (_| |\\ V /  __/",
                "  |______\\__,_|___/\\__, |_____/ \\__,_| \\_/ \\___|",
                "                    __/ |                        ",
                "                   |___/                         "
            ];

            for (int i = 0; i < logo.Length; i++)
            {
                WriteCentered(logo[i], i % 2 == 0 ? ConsoleColor.Cyan : ConsoleColor.DarkCyan);
            }

            Console.WriteLine();
            TypewriteCentered("SECURE YOUR DATA. COMMAND YOUR BACKUPS.", ConsoleColor.DarkGray, 5);
            AnimateProgressBar("Loading interface", 22, 8, ConsoleColor.DarkCyan);
            Thread.Sleep(120);
        }

        private MainMenuAction ShowMainMenu()
        {
            while (true)
            {
                RenderMainMenu();
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (TryGetNumberSelection(keyInfo, MenuEntries.Length, out int selectedByNumber))
                {
                    _mainMenuIndex = selectedByNumber;
                    return MenuEntries[_mainMenuIndex].Action;
                }

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        _mainMenuIndex = WrapIndex(_mainMenuIndex - 1, MenuEntries.Length);
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        _mainMenuIndex = WrapIndex(_mainMenuIndex + 1, MenuEntries.Length);
                        break;
                    case ConsoleKey.Enter:
                        return MenuEntries[_mainMenuIndex].Action;
                    case ConsoleKey.Escape:
                        return MainMenuAction.Exit;
                }
            }
        }

        private void RenderMainMenu()
        {
            Console.Clear();

            bool isFrench = IsFrenchUi();
            int width = GetPanelWidth();
            string title = _vm.GetText("MainMenuTitle");
            string shortcutHelp = isFrench
                ? "Fleches/W-S: naviguer, Entree: valider, Esc: quitter."
                : "Arrow/W-S: navigate, Enter: select, Esc: exit.";

            WriteCentered(BuildFrameLine(width, '='), ConsoleColor.DarkCyan);
            WriteCentered("|" + CenterText(title, width - 2) + "|", ConsoleColor.Cyan);
            WriteCentered("|" + CenterText(shortcutHelp, width - 2) + "|", ConsoleColor.DarkGray);
            WriteCentered(BuildFrameLine(width, '='), ConsoleColor.DarkCyan);
            Console.WriteLine();

            for (int i = 0; i < MenuEntries.Length; i++)
            {
                string optionText = RemoveNumberPrefix(_vm.GetText(MenuEntries[i].TextKey));
                bool isSelected = i == _mainMenuIndex;
                WriteMenuLine($"{i + 1}. {optionText}", width, isSelected);
            }

            Console.WriteLine();
            RenderCatToolPanel(width, MenuEntries[_mainMenuIndex].Action, isFrench);
            Console.WriteLine();

        }

        private void WriteMenuLine(string text, int width, bool isSelected)
        {
            int maxTextLength = width - 8;
            string clipped = text.Length > maxTextLength ? text[..maxTextLength] : text;
            string prefix = isSelected ? "> " : "  ";
            string line = $"| {prefix}{clipped.PadRight(width - 6)} |";

            if (isSelected)
            {
                WriteCentered(line, ConsoleColor.White, ConsoleColor.DarkCyan);
                return;
            }

            WriteCentered(line, ConsoleColor.Gray);
        }

        private void RenderCatToolPanel(int width, MainMenuAction action, bool isFrench)
        {
            CatTool tool = GetCatTool(action, isFrench);

            WriteCentered(BuildFrameLine(width, '-'), ConsoleColor.DarkBlue);
            WriteCentered("|" + CenterText("EasyCat Assistant", width - 2) + "|", ConsoleColor.Blue);
            WriteCentered("|" + CenterText(tool.Title, width - 2) + "|", ConsoleColor.DarkGray);

            foreach (string line in tool.Art)
            {
                WriteCentered("|" + CenterText(line, width - 2) + "|", ConsoleColor.Yellow);
            }

            WriteCentered("|" + CenterText(tool.Hint, width - 2) + "|", ConsoleColor.DarkGray);
            WriteCentered(BuildFrameLine(width, '-'), ConsoleColor.DarkBlue);
        }

        private CatTool GetCatTool(MainMenuAction action, bool isFrench)
        {
            return action switch
            {
                MainMenuAction.ListJobs => new CatTool(
                    isFrench ? "Mode scan: inventaire des sauvegardes" : "Scan mode: backup inventory",
                    [
                        " /\\_/\\      .--.",
                        "( o.o )    /_._/",
                        " > ^ <      ||"
                    ],
                    isFrench ? "Le chat inspecte tous les jobs." : "The cat inspects all jobs."
                ),
                MainMenuAction.CreateJob => new CatTool(
                    isFrench ? "Mode creation: kit de configuration" : "Create mode: setup toolkit",
                    [
                        " /\\_/\\      ____",
                        "( ^.^ )    | ++ |",
                        " / > <\\    |____|"
                    ],
                    isFrench ? "Le chat prepare un nouveau job." : "The cat prepares a new job."
                ),
                MainMenuAction.ExecuteJobs => new CatTool(
                    isFrench ? "Mode execution: lanceur rapide" : "Execute mode: quick launcher",
                    [
                        " /\\_/\\      __>",
                        "( o.o )    /_/|",
                        " > ^ <     \\_\\|"
                    ],
                    isFrench ? "Le chat lance les sauvegardes." : "The cat launches backups."
                ),
                MainMenuAction.ModifyJob => new CatTool(
                    isFrench ? "Mode edition: outils de precision" : "Edit mode: precision tools",
                    [
                        " /\\_/\\      _",
                        "( o.o )    /_|-",
                        " > ^ <      /\\"
                    ],
                    isFrench ? "Le chat ajuste le job existant." : "The cat adjusts the existing job."
                ),
                MainMenuAction.DeleteJob => new CatTool(
                    isFrench ? "Mode suppression: bombe de demolition" : "Delete mode: demolition bomb",
                    [
                        " /\\_/\\     .-.-.",
                        "( >.< )   ( BOOM )",
                        " > ^ <     `-'-'"
                    ],
                    isFrench ? "Le chat est pret a supprimer ce job." : "The cat is ready to delete this job."
                ),
                MainMenuAction.ChangeLanguage => new CatTool(
                    isFrench ? "Mode langue: commutateur EN/FR" : "Language mode: EN/FR switcher",
                    [
                        " /\\_/\\    [EN|FR]",
                        "( o.o )     /||\\",
                        " > ^ <      /__\\"
                    ],
                    isFrench ? "Le chat gere la langue de l'interface." : "The cat manages UI language."
                ),
                _ => new CatTool(
                    isFrench ? "Mode sortie: fermeture de session" : "Exit mode: session closing",
                    [
                        " /\\_/\\      o/",
                        "( -.- )    /|",
                        " > ^ <     / \\"
                    ],
                    isFrench ? "Le chat ferme proprement l'application." : "The cat closes the app safely."
                )
            };
        }

        private void ShowActionScreen(string textKey)
        {
            bool isFrench = IsFrenchUi();
            string actionName = RemoveNumberPrefix(_vm.GetText(textKey));

            Console.Clear();
            int width = GetPanelWidth();

            WriteCentered(BuildFrameLine(width, '-'), ConsoleColor.DarkCyan);
            WriteCentered("|" + CenterText(actionName, width - 2) + "|", ConsoleColor.Cyan);
            WriteCentered(BuildFrameLine(width, '-'), ConsoleColor.DarkCyan);
            Console.WriteLine();

            AnimateProgressBar(isFrench ? "Preparation" : "Preparing", 24, 9, ConsoleColor.DarkCyan);
            Console.WriteLine();
            WriteCentered(
                isFrench
                    ? "Module pret pour la logique metier."
                    : "Module ready for backend business logic.",
                ConsoleColor.DarkGray
            );

            Pause();
        }

        private void RequestLanguageAtStartup()
        {
            ChooseLanguage(isStartup: true);
        }

        private void ChangeLanguage()
        {
            ChooseLanguage(isStartup: false);
        }

        private void ChooseLanguage(bool isStartup)
        {
            List<string> supportedLanguages = [.. _vm.GetSupportedLanguages()];
            if (supportedLanguages.Count == 0)
            {
                supportedLanguages.Add("en");
                supportedLanguages.Add("fr");
            }

            int selectedIndex = 0;
            while (true)
            {
                RenderLanguageMenu(supportedLanguages, selectedIndex, isStartup);
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (TryGetNumberSelection(keyInfo, supportedLanguages.Count, out int numberSelection))
                {
                    selectedIndex = numberSelection;
                    if (_vm.ChangeLanguage(supportedLanguages[selectedIndex]))
                    {
                        return;
                    }

                    ShowLanguageError();
                    continue;
                }

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        selectedIndex = WrapIndex(selectedIndex - 1, supportedLanguages.Count);
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        selectedIndex = WrapIndex(selectedIndex + 1, supportedLanguages.Count);
                        break;
                    case ConsoleKey.Enter:
                        if (_vm.ChangeLanguage(supportedLanguages[selectedIndex]))
                        {
                            return;
                        }

                        ShowLanguageError();
                        break;
                    case ConsoleKey.Escape:
                        if (!isStartup)
                        {
                            return;
                        }

                        break;
                }
            }
        }

        private void RenderLanguageMenu(IReadOnlyList<string> languages, int selectedIndex, bool isStartup)
        {
            Console.Clear();
            int width = GetPanelWidth();

            string title = isStartup ? "LANGUAGE SETUP" : "LANGUAGE SWITCH";
            string subtitle = isStartup
                ? "Choose your language / Choisissez votre langue"
                : "Change language / Changer la langue";

            WriteCentered(BuildFrameLine(width, '='), ConsoleColor.DarkCyan);
            WriteCentered("|" + CenterText(title, width - 2) + "|", ConsoleColor.Cyan);
            WriteCentered("|" + CenterText(subtitle, width - 2) + "|", ConsoleColor.DarkGray);
            WriteCentered(BuildFrameLine(width, '='), ConsoleColor.DarkCyan);
            Console.WriteLine();

            for (int i = 0; i < languages.Count; i++)
            {
                string optionText = $"{i + 1}. {GetLanguageLabel(languages[i])} ({languages[i].ToUpperInvariant()})";
                WriteMenuLine(optionText, width, i == selectedIndex);
            }

            Console.WriteLine();
            WriteCentered(
                isStartup
                    ? "Arrow/W-S + Enter"
                    : "Arrow/W-S + Enter, Esc to cancel",
                ConsoleColor.DarkGray
            );
        }

        private void ShowLanguageError()
        {
            WriteCentered(_vm.GetText("ErrorInvalidOption"), ConsoleColor.White, ConsoleColor.DarkRed);
            Thread.Sleep(420);
        }

        private void Pause()
        {
            Console.WriteLine();
            WriteCentered(
                IsFrenchUi()
                    ? "Press Enter / Entree pour continuer..."
                    : "Press Enter to continue...",
                ConsoleColor.DarkGray
            );
            Console.ReadLine();
        }

        private bool IsFrenchUi()
        {
            string prompt = _vm.GetText("MenuPrompt");
            return prompt.Contains("Selectionnez", StringComparison.OrdinalIgnoreCase)
                || prompt.Contains("Sélectionnez", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetLanguageLabel(string languageCode)
        {
            return languageCode.ToLowerInvariant() switch
            {
                "en" => "English",
                "fr" => "Francais",
                _ => languageCode
            };
        }

        private static int WrapIndex(int value, int max)
        {
            if (max <= 0)
            {
                return 0;
            }

            if (value < 0)
            {
                return max - 1;
            }

            if (value >= max)
            {
                return 0;
            }

            return value;
        }

        private static bool TryGetNumberSelection(ConsoleKeyInfo keyInfo, int count, out int index)
        {
            if (keyInfo.Key >= ConsoleKey.D1 && keyInfo.Key <= ConsoleKey.D9)
            {
                int value = keyInfo.Key - ConsoleKey.D1;
                if (value < count)
                {
                    index = value;
                    return true;
                }
            }

            if (keyInfo.Key >= ConsoleKey.NumPad1 && keyInfo.Key <= ConsoleKey.NumPad9)
            {
                int value = keyInfo.Key - ConsoleKey.NumPad1;
                if (value < count)
                {
                    index = value;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private static int GetPanelWidth()
        {
            int windowWidth;
            try
            {
                windowWidth = Console.WindowWidth;
            }
            catch
            {
                windowWidth = 100;
            }

            int width = windowWidth - 4;
            if (width < 52)
            {
                return 52;
            }

            return width > 96 ? 96 : width;
        }

        private static string BuildFrameLine(int width, char character)
        {
            if (width < 2)
            {
                return "++";
            }

            return "+" + new string(character, width - 2) + "+";
        }

        private static string RemoveNumberPrefix(string optionText)
        {
            int dotIndex = optionText.IndexOf('.');
            if (dotIndex >= 0 && dotIndex <= 3)
            {
                return optionText[(dotIndex + 1)..].Trim();
            }

            return optionText.Trim();
        }

        private static string CenterText(string text, int width)
        {
            if (text.Length >= width)
            {
                return text[..width];
            }

            int leftPadding = (width - text.Length) / 2;
            int rightPadding = width - text.Length - leftPadding;
            return new string(' ', leftPadding) + text + new string(' ', rightPadding);
        }

        private static void TypewriteCentered(string text, ConsoleColor color, int delayMs)
        {
            int windowWidth;
            try
            {
                windowWidth = Console.WindowWidth;
            }
            catch
            {
                windowWidth = 120;
            }

            string output = text.Length > windowWidth ? text[..windowWidth] : text;
            int leftPadding = Math.Max(0, (windowWidth - output.Length) / 2);
            Console.ForegroundColor = color;
            Console.Write(new string(' ', leftPadding));
            foreach (char character in output)
            {
                Console.Write(character);
                Thread.Sleep(delayMs);
            }

            Console.WriteLine();
            Console.ResetColor();
        }

        private static void AnimateProgressBar(string label, int width, int delayMs, ConsoleColor color)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            for (int i = 0; i <= width; i++)
            {
                int percent = (int)((i / (double)width) * 100);
                string bar = "[" + new string('#', i) + new string('.', width - i) + $"] {percent,3}%";
                Console.ForegroundColor = color;
                Console.Write($"\r{label} {bar}");
                Thread.Sleep(delayMs);
            }

            Console.ForegroundColor = originalColor;
            Console.WriteLine();
        }

        private static void WriteCentered(string text, ConsoleColor foreground)
        {
            WriteCentered(text, foreground, Console.BackgroundColor);
        }

        private static void WriteCentered(string text, ConsoleColor foreground, ConsoleColor background)
        {
            int windowWidth;
            try
            {
                windowWidth = Console.WindowWidth;
            }
            catch
            {
                windowWidth = 120;
            }

            string output = text;
            if (output.Length > windowWidth)
            {
                output = output[..windowWidth];
            }

            int leftPadding = Math.Max(0, (windowWidth - output.Length) / 2);
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.WriteLine(new string(' ', leftPadding) + output);
            Console.ResetColor();
        }

        private static void TrySetCursorVisible(bool isVisible)
        {
            try
            {
                Console.CursorVisible = isVisible;
            }
            catch
            {
            }
        }

        private readonly record struct MenuEntry(string TextKey, MainMenuAction Action);

        private readonly record struct CatTool(string Title, string[] Art, string Hint);

        private enum MainMenuAction
        {
            ListJobs,
            CreateJob,
            ExecuteJobs,
            ModifyJob,
            DeleteJob,
            ChangeLanguage,
            Exit
        }
    }
}
