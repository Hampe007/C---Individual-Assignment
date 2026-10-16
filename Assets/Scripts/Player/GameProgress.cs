using System;
using System.IO;
using UnityEngine;

public static class GameProgress
{
    private static ProgressSaveData data;

    public static string SavePath => Path.Combine(Application.persistentDataPath, "progress.json");
    public static int HighScore => Data.highScore;
    public static bool TutorialCompleted => Data.tutorialCompleted;
    public static bool TutorialPromptDismissed => Data.tutorialPromptDismissed;

    private static ProgressSaveData Data
    {
        get
        {
            if (data != null)
            {
                return data;
            }

            data = ProgressSaveFile.Load(SavePath);
            if (!File.Exists(SavePath))
            {
                data.highScore = Mathf.Max(0, PlayerPrefs.GetInt("HighScore", 0));
                data.tutorialCompleted = PlayerPrefs.GetInt("Tutorial.Completed", 0) != 0;
                // The old menu checked whether the key existed, including a stored value of zero.
                data.tutorialPromptDismissed = PlayerPrefs.HasKey("LivingMenu.TutorialPromptDismissed");
                if (ProgressSaveFile.Save(SavePath, data))
                {
                    PlayerPrefs.DeleteKey("HighScore");
                    PlayerPrefs.DeleteKey("Tutorial.Completed");
                    PlayerPrefs.DeleteKey("LivingMenu.TutorialPromptDismissed");
                    PlayerPrefs.Save();
                }
            }

            return data;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        data = null;
    }

    public static void SaveHighScore(int score)
    {
        if (score <= Data.highScore)
        {
            return;
        }

        Data.highScore = score;
        ProgressSaveFile.Save(SavePath, Data);
    }

    public static void CompleteTutorial()
    {
        Data.tutorialCompleted = true;
        ProgressSaveFile.Save(SavePath, Data);
    }

    public static void DismissTutorialPrompt()
    {
        Data.tutorialPromptDismissed = true;
        ProgressSaveFile.Save(SavePath, Data);
    }
}

[Serializable]
internal sealed class ProgressSaveData
{
    public int formatVersion = 1;
    public int highScore;
    public bool tutorialCompleted;
    public bool tutorialPromptDismissed;
}

internal static class ProgressSaveFile
{
    public static ProgressSaveData Load(string path)
    {
        if (!File.Exists(path))
        {
            return new ProgressSaveData();
        }

        if (TryLoad(path, out ProgressSaveData saved))
        {
            return saved;
        }

        if (TryLoad(path + ".bak", out saved))
        {
            return saved;
        }

        Debug.LogWarning("Progress could not be loaded. Starting with default progress.");
        return new ProgressSaveData();
    }

    private static bool TryLoad(string path, out ProgressSaveData saved)
    {
        saved = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            saved = JsonUtility.FromJson<ProgressSaveData>(File.ReadAllText(path));
            if (saved != null && saved.formatVersion == 1 && saved.highScore >= 0)
            {
                return true;
            }

            Debug.LogWarning($"Invalid progress data in '{path}'.");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            Debug.LogWarning($"Could not read progress from '{path}': {exception.Message}");
        }

        return false;
    }

    public static bool Save(string path, ProgressSaveData saved)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(saved, true));
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, path + ".bak");
            }
            else
            {
                File.Move(temporaryPath, path);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Could not save progress to '{path}': {exception.Message}");
            return false;
        }
    }
}
