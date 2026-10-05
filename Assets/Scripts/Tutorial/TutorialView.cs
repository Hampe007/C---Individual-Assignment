using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TutorialView : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text instruction;
    [SerializeField] private TMP_Text progress;
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button completionMenuButton;

    public void Initialize(TutorialDirector director)
    {
        replayButton.onClick.AddListener(director.Replay);
        completionMenuButton.onClick.AddListener(director.ReturnToMenu);
        completionPanel.SetActive(false);
    }

    public void ShowLesson(TutorialLesson lesson, int index, int total)
    {
        title.text = $"{index + 1}/{total}  {lesson.title.ToUpperInvariant()}";
        instruction.text = lesson.instruction;
        progress.text = string.Empty;
    }

    public void ShowProgress(string value)
    {
        progress.text = value;
    }

    public void ShowCompletion()
    {
        completionPanel.SetActive(true);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(replayButton.gameObject);
        }
    }
}
