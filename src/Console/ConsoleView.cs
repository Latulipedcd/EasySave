using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using EasySave.Application.ViewModels;

namespace EasySave.ConsoleApp
{
    public class ConsoleView
    {
        // Placeholder: In a real MVVM setup, this View would have access to the ViewModel
        // private MainViewModel _vm;

        private MainViewModel _vm;

        public ConsoleView()
        {
            _vm = new MainViewModel();
        }

        public void Start()
        {
            // Initial setup (Language selection, loading jobs)
            // TODO: Call _vm.LoadConfig(); 

            bool running = true;
            while (running)
            {
                DisplayMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        // List Backup Jobs
                        break;
                    case "2":
                        // Create a Backup Job
                        break;
                    case "3":
                        // Execute Backup Job(s)
                        break;
                    case "4":
                        // Modify Backup Job
                        break;
                    case "5":
                        // Delete Backup Job
                        break;
                    case "6":
                        ChangeLanguage();
                        break;
                    case "7":
                        running = false;
                        break;
                    default:
                        Console.WriteLine(_vm.GetText("ErrorInvalidOption"));
                        break;
                }
            }
        }

        private void DisplayMenu()
        {
            Console.Clear();

            // 1. Get the Title
            Console.WriteLine(_vm.GetText("MainMenuTitle"));

            // 2. Get the Options
            Console.WriteLine(_vm.GetText("MenuOptionList"));
            Console.WriteLine(_vm.GetText("MenuOptionCreate"));
            Console.WriteLine(_vm.GetText("MenuOptionExecute"));
            Console.WriteLine(_vm.GetText("MenuOptionModify"));
            Console.WriteLine(_vm.GetText("MenuOptionDelete"));
            Console.WriteLine(_vm.GetText("MenuOptionLang"));
            Console.WriteLine(_vm.GetText("MenuOptionExit"));

            // 3. Get the Prompt
            Console.Write(_vm.GetText("MenuPrompt"));
        }

        private void ChangeLanguage()
        {
            Console.WriteLine("\n1. English");
            Console.WriteLine("2. Français");
            Console.Write("Choice: ");
            string choice = Console.ReadLine();

            if (choice == "1") _vm.ChangeLanguage("en");
            if (choice == "2") _vm.ChangeLanguage("fr");

            // The loop in Start() will call DisplayMenu() again immediately, 
            // showing the new language.
        }
    }
}