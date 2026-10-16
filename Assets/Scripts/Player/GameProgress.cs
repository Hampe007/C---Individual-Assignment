using System;
using System.IO;
using UnityEngine;

public static class GameProgress
{
    private static ProgressSaveData data;

    public static string SavePath => Path.Combine(Application.persistentDataPath, "progress.json");
    public static int HighScore => Data.HighScore;
    public static bool TutorialCompleted => Data.TutorialCompleted;
    public static bool TutorialPromptDismissed => Data.TutorialPromptDismissed;

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
                data.HighScore = Mathf.Max(0, PlayerPrefs.GetInt("HighScore", 0));
                data.TutorialCompleted = PlayerPrefs.GetInt("Tutorial.Completed", 0) != 0;
                // The old menu checked whether the key existed, including a stored value of zero.
                data.TutorialPromptDismissed = PlayerPrefs.HasKey("LivingMenu.TutorialPromptDismissed");
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
        if (score <= Data.HighScore)
        {
            return;
        }

        Data.HighScore = score;
        ProgressSaveFile.Save(SavePath, Data);
    }

    public static void CompleteTutorial()
    {
        Data.TutorialCompleted = true;
        ProgressSaveFile.Save(SavePath, Data);
    }

    public static void DismissTutorialPrompt()
    {
        Data.TutorialPromptDismissed = true;
        ProgressSaveFile.Save(SavePath, Data);
    }
}

[Serializable]
internal sealed class ProgressSaveData
{
    [SerializeField] private int formatVersion = 1;
    [SerializeField] private int highScore;
    [SerializeField] private bool tutorialCompleted;
    [SerializeField] private bool tutorialPromptDismissed;

    public int FormatVersion { get => formatVersion; set => formatVersion = value; }
    public int HighScore { get => highScore; set => highScore = value; }
    public bool TutorialCompleted { get => tutorialCompleted; set => tutorialCompleted = value; }
    public bool TutorialPromptDismissed { get => tutorialPromptDismissed; set => tutorialPromptDismissed = value; }
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
            if (saved != null && saved.FormatVersion == 1 && saved.HighScore >= 0)
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
