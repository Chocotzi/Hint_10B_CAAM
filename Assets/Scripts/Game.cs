
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public static Game obj;

    public int maxLives = 3;

    public bool gamePaused = false;
    public int Score { get; private set; }

    private bool reloadRequested;

    private void Awake()
    {
        obj = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        gamePaused = false;
        Time.timeScale = 1f;
    }

    public void addScore(int scoreGive)
    {
        Score += scoreGive;
    }

    public void gameOver()
    {
        if (reloadRequested) return;

        reloadRequested = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (obj == this)
            obj = null;
    }
}
