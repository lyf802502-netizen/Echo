using System.Collections.Generic;
using Game.Rhythm.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class PauseButton : MonoBehaviour
{
    public List<Sprite> sprites;
    public RhythmSessionController rhythmGameController;

    private Button button;
    private Image image;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayOrPauseMusic);
        image = GetComponent<Image>();
    }

    private void PlayOrPauseMusic()
    {
        if (rhythmGameController == null)
        {
            return;
        }

        rhythmGameController.TogglePause();
        if (rhythmGameController.isPauseState)
        {
            image.sprite = sprites[1];
        }
        else
        {
            image.sprite = sprites[0];
        }
    }
}
