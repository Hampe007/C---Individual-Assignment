using UnityEngine;

/// <summary>Shows a short, bright warning while a Puglin prepares a volley.</summary>
public sealed class EnemySpitTelegraph : MonoBehaviour
{
    [SerializeField] private ParticleSystem _warningParticles;
    [SerializeField] private Transform _body;
    private bool _showing;
    private Vector3 _restPosition;
    private Vector3 _restScale;

    private void Awake()
    {
        if (_body == null) return;
        _restPosition = _body.localPosition;
        _restScale = _body.localScale;
    }

    private void Update()
    {
        if (_body == null || Time.timeScale <= 0f) return;
        float bob = Mathf.Sin(Time.time * 3.5f) * 0.045f;
        _body.localPosition = _restPosition + Vector3.up * bob;
        float pulse = _showing ? 1.07f + Mathf.Sin(Time.time * 18f) * 0.035f : 1f;
        _body.localScale = _restScale * pulse;
    }

    public void SetWarning(bool showing)
    {
        if (_warningParticles == null || _showing == showing) return;
        _showing = showing;
        if (showing) _warningParticles.Play(true);
        else _warningParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDisable()
    {
        _showing = false;
        if (_body != null)
        {
            _body.localPosition = _restPosition;
            _body.localScale = _restScale;
        }
        if (_warningParticles != null)
            _warningParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
