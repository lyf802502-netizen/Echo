using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameQuitButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Button>().onClick.AddListener(QuitGame);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}
