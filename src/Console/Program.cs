using Core.Enums;
using Core.Models;
using Core.Services;

class Program
{
    static void Main()
//This is an harcoded test
    {
        var job = new BackupJob(
            "TestBackup",
            @"C:\Users\latul\Desktop\projets\test",
            @"C:\Users\latul\Desktop\projets\result",
            BackupType.Full
        );

        //var logService = new LogService();
        var fileService= new FileService();
        var copyService= new CopyService();
        var backupService = new BackupService(fileService, copyService);

        var state = backupService.ExecuteBackup(job);

        Console.WriteLine($"Backup terminé : {state.Status}");
        Console.ReadKey();
    }
}
