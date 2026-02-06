using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;

class Program
{
    static void Main()
//This is an harcoded test
    {

        //var logService = new LogService();
        var fileService= new FileService();
        var copyService= new CopyService();
        var backupService = new BackupService(fileService, copyService);


        Console.WriteLine("=== EasySave – Test BackupJobRepository ===");

        IJobStorage storage = new AppDataJobStorage();
        IBackupJobRepository repository = new BackupJobRepository(storage);

        try
        {
            // Nettoyage pour tests répétés
            Console.WriteLine("Suppression des jobs existants...");
            foreach (var job in repository.GetAll())
            {
                repository.Delete(job.Name);
            }

            // Création de 5 jobs
            Console.WriteLine("Création de 5 jobs...");
            for (int i = 1; i <= 5; i++)
            {
                var job = new BackupJob(
                    $"Job{i}",
                    $@"C:\Source{i}",
                    $@"D:\Target{i}",
                    BackupType.Full
                );

                repository.Add(job);
                Console.WriteLine($"Job{i} créé");
            }

            // Tentative de création d’un 6e job (doit échouer)
            Console.WriteLine("Tentative de création d’un 6e job...");
            repository.Add(new BackupJob(
                "Job6",
                @"C:\Source6",
                @"D:\Target6",
                BackupType.Differencial
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur attendue : {ex.Message}");
        }

        // Liste des jobs
        Console.WriteLine("\nJobs existants :");
        foreach (var job in repository.GetAll())
        {
            Console.WriteLine($"- {job.Name} | {job.Type}");
        }

        // Modification d’un job
        Console.WriteLine("\nModification de Job3...");
        var updatedJob = new BackupJob(
            "Job3",
            @"C:\NewSource",
            @"E:\NewTarget",
            BackupType.Differenciate
        );
        repository.Update(updatedJob);

        // Suppression d’un job
        Console.WriteLine("Suppression de Job2...");
        repository.Delete("Job2");

        // État final
        Console.WriteLine("\nJobs après modification :");
        foreach (var job in repository.GetAll())
        {
            Console.WriteLine($"- {job.Name} | {job.SourceDirectory} -> {job.TargetDirectory} | {job.Type}");
        }

        Console.WriteLine("\nTests terminés.");
        repository.Add(new BackupJob(
                "JobT",
                @"C:\Users\latul\Desktop\projets\test",
                @"C:\Users\latul\Desktop\projets\result",
                BackupType.Differenciate
            ));

        var job2 = repository
          .GetAll()
          .FirstOrDefault(j => j.Name == "JobT");

        if (job2 == null)
        {
            Console.WriteLine("Le job 'Job1' n'existe pas dans le fichier JSON.");
            return;
        }

        Console.WriteLine($"Job trouvé : {job2.Name}");
        Console.WriteLine("Exécution de la sauvegarde...");








        var state = backupService.ExecuteBackup(job2);

        Console.WriteLine($"Backup terminé : {state.Status}");
        Console.ReadKey();
    }
}