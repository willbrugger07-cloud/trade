using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Static helper — call NotificationSystem.Show("msg") from anywhere.
/// Requires a canvas singleton in the scene with a TMP_Text reference.
/// </summary>
public class NotificationSystem : MonoBehaviour
{
    private static NotificationSystem _instance;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private float _displaySeconds = 3f;

    private Coroutine _current;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        _group.alpha = 0f;
    }

    public static void Show(string message)
    {
        if (_instance == null) { Debug.Log($"[Notification] {message}"); return; }
        _instance.ShowInternal(message);
    }

    private void ShowInternal(string message)
    {
        _label.text = message;
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        _group.alpha = 1f;
        yield return new WaitForSeconds(_displaySeconds);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            _group.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }
        _group.alpha = 0f;
    }
}
