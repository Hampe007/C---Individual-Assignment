using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TutorialDirector : MonoBehaviour
{
    private const string CompletionKey = "Tutorial.Completed";

    [SerializeField] private TutorialSettings settings;
    [SerializeField] private TutorialView view;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] markers;
    [SerializeField] private GameObject markerVisual;
    [SerializeField] private HordeManager horde;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerProgression progression;
    [SerializeField] private SkillTreeProgression upgrades;
    [SerializeField, Min(0.5f)] private float markerRadius = 1.5f;
    [SerializeField, Min(1)] private int maximumActiveEnemies = 3;

    private int lessonIndex;
    private int defeated;
    private int spawned;
    private float remaining;
    private float spawnRetry;
    private bool finished;

    private void Awake()
    {
        if (settings == null || settings.lessons == null || settings.lessons.Length == 0 ||
            settings.practiceEnemy == null || view == null || player == null || horde == null ||
            health == null || progression == null || upgrades == null)
        {
            Debug.LogError("TutorialDirector is missing required references.", this);
            enabled = false;
            return;
        }

        view.Initialize(this);
    }

    private void OnEnable()
    {
        if (horde != null)
        {
            horde.EnemyDied += OnEnemyDied;
        }

        if (health != null)
        {
            health.Died += OnPlayerDied;
        }

        if (upgrades != null)
        {
            upgrades.UpgradePurchased += OnUpgradePurchased;
        }

        if (progression != null)
        {
            progression.LeveledUp += OnLeveledUp;
        }
    }

    private void OnDisable()
    {
        if (horde != null)
        {
            horde.EnemyDied -= OnEnemyDied;
        }

        if (health != null)
        {
            health.Died -= OnPlayerDied;
        }

        if (upgrades != null)
        {
            upgrades.UpgradePurchased -= OnUpgradePurchased;
        }

        if (progression != null)
        {
            progression.LeveledUp -= OnLeveledUp;
        }
    }

    private void Start()
    {
        BeginLesson();
    }

    private void Update()
    {
        if (finished || Time.timeScale == 0f)
        {
            return;
        }

        TutorialLesson lesson = settings.lessons[lessonIndex];
        switch (lesson.goal)
        {
            case TutorialGoal.ReachMarker:
                if (markers == null || lesson.markerIndex >= markers.Length || markers[lesson.markerIndex] == null)
                {
                    Debug.LogError("Tutorial marker is missing.", this);
                    enabled = false;
                    return;
                }

                Vector3 offset = player.position - markers[lesson.markerIndex].position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= markerRadius * markerRadius)
                {
                    Advance();
                }
                break;
            case TutorialGoal.DefeatEnemies:
            case TutorialGoal.ReachLevel:
                ReplenishEnemies(lesson.targetCount);
                if (lesson.goal == TutorialGoal.ReachLevel && progression.Level >= 2)
                {
                    Advance();
                }
                break;
            case TutorialGoal.Survive:
                ReplenishEnemies(lesson.targetCount);
                remaining -= Time.deltaTime;
                view.ShowProgress($"{Mathf.CeilToInt(remaining)} seconds left");
                if (remaining <= 0f)
                {
                    Advance();
                }
                break;
        }
    }

    private void BeginLesson()
    {
        TutorialLesson lesson = settings.lessons[lessonIndex];
        defeated = 0;
        spawned = 0;
        remaining = lesson.duration;
        spawnRetry = 0f;
        view.ShowLesson(lesson, lessonIndex, settings.lessons.Length);

        if (markerVisual != null)
        {
            bool showMarker = lesson.goal == TutorialGoal.ReachMarker;
            markerVisual.SetActive(showMarker);
            if (showMarker && lesson.markerIndex < markers.Length && markers[lesson.markerIndex] != null)
            {
                markerVisual.transform.position = markers[lesson.markerIndex].position + Vector3.up * 0.05f;
            }
        }

        if (lesson.goal == TutorialGoal.DefeatEnemies)
        {
            view.ShowProgress($"0/{lesson.targetCount} defeated");
        }
        else if (lesson.goal == TutorialGoal.ReachLevel)
        {
            view.ShowProgress($"Level {progression.Level}/2");
        }
    }

    private void ReplenishEnemies(int targetCount)
    {
        if (!horde.IsReady || Time.time < spawnRetry)
        {
            return;
        }

        TutorialLesson lesson = settings.lessons[lessonIndex];
        if (lesson.goal == TutorialGoal.DefeatEnemies && spawned >= targetCount)
        {
            return;
        }

        if (horde.ActiveEnemyCount >= maximumActiveEnemies)
        {
            return;
        }

        for (int attempt = 0; attempt < 16; attempt++)
        {
            float angle = (spawned * 137f + attempt * 47f) * Mathf.Deg2Rad;
            float radius = 7f + (attempt % 3) * 1.5f;
            Vector3 position = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (!horde.TrySpawnEnemyAt(settings.practiceEnemy, position))
            {
                continue;
            }

            spawned++;
            spawnRetry = Time.time + (lesson.goal == TutorialGoal.Survive ? 3f : 1f);
            return;
        }

        spawnRetry = Time.time + 1f;
    }

    private void OnEnemyDied(int experience, int score)
    {
        if (finished || lessonIndex >= settings.lessons.Length)
        {
            return;
        }

        TutorialLesson lesson = settings.lessons[lessonIndex];
        if (lesson.goal != TutorialGoal.DefeatEnemies)
        {
            return;
        }

        defeated++;
        view.ShowProgress($"{Mathf.Min(defeated, lesson.targetCount)}/{lesson.targetCount} defeated");
        if (defeated >= lesson.targetCount)
        {
            Advance();
        }
    }

    private void OnUpgradePurchased()
    {
        if (!finished && settings.lessons[lessonIndex].goal == TutorialGoal.ChooseUpgrade)
        {
            Advance();
        }
    }

    private void OnLeveledUp(int level)
    {
        if (!finished && level >= 2 && settings.lessons[lessonIndex].goal == TutorialGoal.ReachLevel)
        {
            Advance();
        }
    }

    private void OnPlayerDied()
    {
        finished = true;
        if (markerVisual != null)
        {
            markerVisual.SetActive(false);
        }
    }

    private void Advance()
    {
        lessonIndex++;
        if (lessonIndex < settings.lessons.Length)
        {
            BeginLesson();
            return;
        }

        finished = true;
        if (markerVisual != null)
        {
            markerVisual.SetActive(false);
        }
        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();
        Time.timeScale = 0f;
        view.ShowCompletion();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(settings.menuScene);
    }

    public void Replay()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(SceneManager.GetActiveScene().name);
    }
}
