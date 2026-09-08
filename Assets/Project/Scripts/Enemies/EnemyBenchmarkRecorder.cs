using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class EnemyBenchmarkRecorder : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _warmupSeconds = 3f;
    [SerializeField, Min(1f)] private float _testSeconds = 15f;
    [SerializeField] private HordeManager _hordeManager;

    private readonly List<float> _frameTimes = new();
    private float _timer;
    private bool _recording;
    private bool _finished;

    private void Start()
    {
        if (_hordeManager != null)
            return;

        Debug.LogError("EnemyBenchmarkRecorder requires a HordeManager.");
        enabled = false;
    }

    private void Update()
    {
        if (_finished)
            return;

        _timer += Time.unscaledDeltaTime;

        if (!_recording)
        {
            if (_timer < _warmupSeconds)
                return;

            _timer = 0f;
            _recording = true;
            _frameTimes.Clear();
            Debug.Log("Benchmark recording started.");
            return;
        }

        _frameTimes.Add(Time.unscaledDeltaTime * 1000f);

        if (_timer >= _testSeconds)
            FinishBenchmark();
    }

    private void FinishBenchmark()
    {
        _finished = true;

        try
        {
            if (_frameTimes.Count == 0)
                throw new InvalidOperationException("No frame data was recorded.");

            _frameTimes.Sort();

            float average = Average();
            float median = Percentile(0.50f);
            float p95 = Percentile(0.95f);
            float p99 = Percentile(0.99f);
            float worst = _frameTimes[^1];
            int above10 = CountAbove(10f);
            int above1667 = CountAbove(16.67f);

            string fileName = $"enemy_benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = Path.Combine(Application.persistentDataPath, fileName);

            string csv =
                "enemy_count,test_seconds,frames,average_fps,average_ms,median_ms,p95_ms,p99_ms,worst_ms,frames_over_10ms,frames_over_16_67ms\n" +
                $"{_hordeManager.EnemyCount}," +
                $"{Format(_testSeconds)}," +
                $"{_frameTimes.Count}," +
                $"{Format(1000f / average, 2)}," +
                $"{Format(average)}," +
                $"{Format(median)}," +
                $"{Format(p95)}," +
                $"{Format(p99)}," +
                $"{Format(worst)}," +
                $"{above10}," +
                $"{above1667}\n";

            File.WriteAllText(path, csv);
            Debug.Log($"Benchmark finished.\nSaved to:\n{path}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Benchmark could not be saved: {exception.Message}");
        }
    }

    private float Average()
    {
        float total = 0f;

        foreach (float frameTime in _frameTimes)
            total += frameTime;

        return total / _frameTimes.Count;
    }

    private float Percentile(float percentile)
    {
        int index = Mathf.CeilToInt(_frameTimes.Count * percentile) - 1;
        return _frameTimes[Mathf.Clamp(index, 0, _frameTimes.Count - 1)];
    }

    private int CountAbove(float milliseconds)
    {
        int count = 0;

        foreach (float frameTime in _frameTimes)
        {
            if (frameTime > milliseconds)
                count++;
        }

        return count;
    }

    private static string Format(float value, int decimals = 3)
    {
        return value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
    }
}
