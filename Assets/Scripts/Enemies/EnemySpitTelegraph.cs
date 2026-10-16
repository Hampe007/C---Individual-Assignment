using UnityEngine;

/// <summary>Shows a short, bright warning while a Puglin prepares a volley.</summary>
public sealed class EnemySpitTelegraph : MonoBehaviour
{
    [SerializeField] private ParticleSystem warningParticles;
    [SerializeField] private Transform body;
    private bool showing;
    private Vector3 restPosition;
    private Vector3 restScale;
    private void Awake()
    {
        if (body == null)
        {
            return;
        }

        restPosition = body.localPosition;
        restScale = body.localScale;
    }

    private void Update()
    {
        if (body == null || Time.timeScale <= 0f)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * 3.5f) * 0.045f;
        body.localPosition = restPosition + Vector3.up * bob;
        float pulse = showing ? 1.07f + Mathf.Sin(Time.time * 18f) * 0.035f : 1f;
        body.localScale = restScale * pulse;
    }

    public void SetWarning(bool showing)
    {
        if (warningParticles == null || this.showing == showing)
        {
            return;
        }

        this.showing = showing;
        if (showing)
        {
            warningParticles.Play(true);
        }
        else
        {
            warningParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDisable()
    {
        showing = false;
        if (body != null)
        {
            body.localPosition = restPosition;
            body.localScale = restScale;
        }

        if (warningParticles != null)
        {
            warningParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}