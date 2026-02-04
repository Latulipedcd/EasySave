using Core.Enums;
using Core.Models;
using Core.Services;

class Program
{
    static void Main()
    {
        var job = new BackupJob(
            "TestBackup",
            @"C:\Users\enzo.casse\Documents\test",
            @"C:\Users\enzo.casse\Documents\result",
            BackupType.Full
        );

        //var logService = new LogService();
        var backupService = new BackupService();

        var state = backupService.ExecuteBackup(job);

        Console.WriteLine($"Backup terminé : {state.Status}");
        Console.ReadKey();
    }
}
