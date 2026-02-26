using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Models;
using EasySave.Application.Interfaces;

namespace EasySave.Application.Services;

/// <summary>
/// Reads backup job progress states from the state.json file written by ProgressJsonWriter.
/// </summary>
public class JobStateFileReader : IJobStateReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public List<BackupState>? ReadAllStates()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var statePath = Path.Combine(appData, "EasySave", "Progress", "state.json");

            if (!File.Exists(statePath))
                return null;

            using var fs = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<BackupState>>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading state.json: {ex.Message}");
            return null;
        }
    }
}
