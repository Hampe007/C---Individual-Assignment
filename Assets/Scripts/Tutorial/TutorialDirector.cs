using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TutorialDirector : MonoBehaviour
{
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
        if (settings == null || settings.Lessons == null || settings.Lessons.Length == 0 ||
            settings.PracticeEnemy == null || view == null || player == null || horde == null ||
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

        TutorialLesson lesson = settings.Lessons[lessonIndex];
        switch (lesson.Goal)
        {
            case TutorialGoal.ReachMarker:
                if (markers == null || lesson.MarkerIndex >= markers.Length || markers[lesson.MarkerIndex] == null)
                {
                    Debug.LogError("Tutorial marker is missing.", this);
                    enabled = false;
                    return;
                }

                Vector3 offset = player.position - markers[lesson.MarkerIndex].position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= markerRadius * markerRadius)
                {
                    Advance();
                }
                break;
            case TutorialGoal.DefeatEnemies:
            case TutorialGoal.ReachLevel:
                ReplenishEnemies(lesson.TargetCount);
                if (lesson.Goal == TutorialGoal.ReachLevel && progression.Level >= 1)
                {
                    Advance();
                }
                break;
            case TutorialGoal.Survive:
                ReplenishEnemies(lesson.TargetCount);
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
        TutorialLesson lesson = settings.Lessons[lessonIndex];
        defeated = 0;
        spawned = 0;
        remaining = lesson.Duration;
        spawnRetry = 0f;
        view.ShowLesson(lesson, lessonIndex, settings.Lessons.Length);

        if (markerVisual != null)
        {
            bool showMarker = lesson.Goal == TutorialGoal.ReachMarker;
            markerVisual.SetActive(showMarker);
            if (showMarker && lesson.MarkerIndex < markers.Length && markers[lesson.MarkerIndex] != null)
            {
                markerVisual.transform.position = markers[lesson.MarkerIndex].position + Vector3.up * 0.05f;
            }
        }

        if (lesson.Goal == TutorialGoal.DefeatEnemies)
        {
            view.ShowProgress($"0/{lesson.TargetCount} defeated");
        }
        else if (lesson.Goal == TutorialGoal.ReachLevel)
        {
            view.ShowProgress($"Level {progression.Level}/1");
        }
    }

    private void ReplenishEnemies(int targetCount)
    {
        if (!horde.IsReady || Time.time < spawnRetry)
        {
            return;
        }

        TutorialLesson lesson = settings.Lessons[lessonIndex];
        if (lesson.Goal == TutorialGoal.DefeatEnemies && spawned >= targetCount)
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
            if (!horde.TrySpawnEnemyAt(settings.PracticeEnemy, position))
            {
                continue;
            }

            spawned++;
            spawnRetry = Time.time + (lesson.Goal == TutorialGoal.Survive ? 3f : 1f);
            return;
        }

        spawnRetry = Time.time + 1f;
    }

    private void OnEnemyDied(int experience, int score)
    {
        if (finished || lessonIndex >= settings.Lessons.Length)
        {
            return;
        }

        TutorialLesson lesson = settings.Lessons[lessonIndex];
        if (lesson.Goal != TutorialGoal.DefeatEnemies)
        {
            return;
        }

        defeated++;
        view.ShowProgress($"{Mathf.Min(defeated, lesson.TargetCount)}/{lesson.TargetCount} defeated");
        if (defeated >= lesson.TargetCount)
        {
            Advance();
        }
    }

    private void OnUpgradePurchased()
    {
        if (!finished && settings.Lessons[lessonIndex].Goal == TutorialGoal.ChooseUpgrade)
        {
            Advance();
        }
    }

    private void OnLeveledUp(int level)
    {
        if (!finished && level >= 1 && settings.Lessons[lessonIndex].Goal == TutorialGoal.ReachLevel)
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
        if (lessonIndex < settings.Lessons.Length)
        {
            BeginLesson();
            return;
        }

        finished = true;
        if (markerVisual != null)
        {
            markerVisual.SetActive(false);
        }
        GameProgress.CompleteTutorial();
        Time.timeScale = 0f;
        view.ShowCompletion();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(settings.MenuScene);
    }

    public void Replay()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(SceneManager.GetActiveScene().name);
    }
}
