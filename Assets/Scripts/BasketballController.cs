using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/**
 * Glavni kontroler za Pocket basketball aplikaciju
 */
public class BasketballController : MonoBehaviour
{
    [Header("Game objects")]
    public GameObject ballObject;
    public GameObject hoopObject;
    public GameObject scoreAreaObject;

    [Header("UI objects")]
    public TextMeshPro scoreText;
    public TextMeshPro timeText;
    public TextMeshPro highScoreText;
    public GameObject startStopGameButton;
    public GameObject scrollGridCollection;
    public GameObject scrollableObjectCollection;
    public GameObject scoreboardObject;
    public GameObject ballIndicatorObject;
    public TextMeshPro gameLengthText;
    public GameObject startInfoPanel;
    public GameObject mainMenuPanel;

    [Header("Panels")]
    public GameObject scoreboardPanel;
    public GameObject settingsPanel;

    // helper varijable
    private GameObject scoreEffectObject;
    private float ballDistance = 2f;
    private bool hoopUp = false; // lopta je prošla kroz gornji collider
    private bool hoopDown = false; // lopta je prošla kroz donji collider
    private Game currentGame;
    private float timer = 0f;
    private int highscore = 0;
    private int gameCount = 0;
    private int gameLength = 20; // Početka vrijednost je 20 sekundi
    private bool isNearEnd; // Služi samo za pokretanje zvuka odkucavanja zadnjih 10 sekundi
    private Vector3 lastShootPosition; // Zadnje zabilježena pozicija s koje je igra? bacio loptu

    private void Start()
    {
        // pobrini se da objekti koji trebaju biti isklju?eni u startu stvarno jesu
        scoreEffectObject = scoreAreaObject.transform.Find("ScoreEffect").gameObject;
        scoreEffectObject.SetActive(false);
        scoreboardPanel.SetActive(false);
        scoreboardObject.SetActive(false);
        settingsPanel.SetActive(false);
        ballObject.SetActive(false);
        mainMenuPanel.SetActive(false);
        ballObject.SetActive(false);
        startInfoPanel.SetActive(true);

        timer = gameLength;

        // reorijentiraj scenu prema kameri
        this.transform.root.gameObject.GetComponent<MixedRealitySceneContent>().ReorientContent();

        // odmah pokreni Tap to place kako bi se postavio obru?
        StartHoopPlacement();
    }

    private void Update()
    {
        UpdateGameStats();
    }

    #region Public methods

    /**
     * Pokre?e ili zaustavlja igru
     */
    public void StartStopGame()
    {
        if (currentGame != null) // igra je u tijeku, zaustavi ju
        {
            ResetValues();
        } else
        {
            currentGame = new Game();
            startStopGameButton.GetComponent<ButtonConfigHelper>().MainLabelText = "Stop game";
            startStopGameButton.GetComponent<ButtonConfigHelper>().SetQuadIconByName("IconClose");
        }
    }

    /**
     * Pokre?e 'Tap to place' funkcionalnost za micanje koša.
     * Koš se prilikom pozicioniranja "ljepi" na prostor koji Hololens skenira 
     */
    public void StartHoopPlacement()
    {
        hoopObject.GetComponent<TapToPlace>().enabled = true;
        hoopObject.GetComponent<TapToPlace>().StartPlacement();
    }

    /**
     * Pozicionira loptu ispred igra?a
     */
    public void BringToPlayer()
    {
        // Resetiraj brzinu lopte
        ballObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ballObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        // Dohvati glavnu kameru i pozicioniraj loptu ispred igra?a
        Camera camera = Camera.main;
        ballObject.transform.position = camera.transform.position + camera.transform.forward * ballDistance;
    }

    /**
     * Pali/gasi 'Find the ball' indikator
     */
    public void ToggleBallIndicator()
    {
        if (ballIndicatorObject.activeSelf)
        {
            ballIndicatorObject.SetActive(false);
        } else
        {
            ballIndicatorObject.SetActive(true);
        }
    }

    /**
     * Pali/gasi ekran sa tablicom rezultata
     */
    public void ToggleScoreboard()
    {
        scoreboardPanel.SetActive(!scoreboardPanel.activeSelf);
        if (settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
        }
    }

    /**
     * Pali/gasi ekran sa postavkama
     */
    public void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        if (scoreboardPanel.activeSelf)
        {
            scoreboardPanel.SetActive(false);
        }
    }

    #endregion

    #region Listeners

    /**
     * Deaktivira Tap to place i uklju?uje glavne menije ako je ova metoda pokrenuta u startu aplikacije
     */
    public void OnTapToPlaceEnded()
    {
        hoopObject.GetComponent<TapToPlace>().enabled = false;
        if (startInfoPanel.activeSelf)
        {
            startInfoPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
            BringToPlayer();
            ballObject.SetActive(true);
        }
    }

    /**
     * Osigurava da igra? ne može dobiti poene ako baca loptu ispod koša.
     * Metoda se okida kada lopta pogodi bilo koji od dva collidera.
     */
    public void OnScoreAreaTriggerEnter(OnTriggerDelegation delegation)
    {
        if (delegation.Other.name.Equals("Ball"))
        {
            if (delegation.Caller.name.Equals("ScoreCollider"))
            {
                if (hoopDown)
                {
                    hoopDown = false;
                }
                else
                {
                    hoopUp = true;

                    if (scoreEffectObject.activeSelf)
                    {
                        scoreEffectObject.SetActive(false);
                    }
                    scoreEffectObject.SetActive(true);
                    scoreAreaObject.GetComponent<AudioSource>().Play();

                    if (currentGame != null) // samo kada je igra aktivna
                    {
                        AddScore();
                    }
                }
            }
            else if (delegation.Caller.name.Equals("UnderHoopCollider"))
            {
                if (hoopUp)
                {
                    hoopDown = false;
                    hoopUp = false;
                }
                else
                {
                    hoopDown = true;
                }
            }
        }
    }

    /**
     * Ažurira duljinu igre kada se povla?i slider u postavkama
     */
    public void OnSliderGameLength(SliderEventData eventData)
    {
        gameLength = (int)Math.Round((eventData.NewValue * 100 * 3) + 20); // inkrementiraj za 3 sekunde, minimalna vrijednost je 20 sekundi
        gameLengthText.text ="Game length: " + $"{gameLength:F2}" + "s";
        timer = gameLength;
    }


    /**
     * Zabilježava poziciju s koje je igra? bacio ili ispustio loptu.
     * Ta se pozicija koristi kod ra?unanja radi li se o 1, 2 ili 3 poena kod zabijanja koša.
     */
    public void OnBallHoldEnded()
    {
        // ovdje može i?i ili pozicija kamere ili pozicija lopte
        lastShootPosition = Camera.main.transform.position;
    }

    #endregion

    #region Private methods

    /**
     * Dodaje proslije?enu igru u tablicu rezultata
     */
    private void AddToScoreboard(Game game)
    {
        GameObject scoreboardItem = Instantiate(scoreboardObject, scrollGridCollection.transform);
        scoreboardItem.transform.Find("Number").GetComponent<TextMeshPro>().text = game.number.ToString() + ".";
        scoreboardItem.transform.Find("Score").GetComponent<TextMeshPro>().text = game.score.ToString();
        scoreboardItem.transform.Find("Time").GetComponent<TextMeshPro>().text = game.gameTime.ToString() + "s";
        scoreboardItem.transform.Find("FreeThrows").GetComponent<TextMeshPro>().text = game.freeThrows.ToString();
        scoreboardItem.transform.Find("FieldGoals").GetComponent<TextMeshPro>().text = game.fieldGoals.ToString();
        scoreboardItem.SetActive(true);

        scrollGridCollection.GetComponent<GridObjectCollection>().UpdateCollection();
        scrollableObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
    }

    /**
     * Dodaje poen/e trenutnoj igri na temelju udaljenosti s koje je igra? zabio koš
     */
    private void AddScore()
    {
        // odredi broj poena
        float distance = Vector3.Distance(scoreAreaObject.transform.position, lastShootPosition);
        int score = 0;
        if (distance < 3)
        {
            score = 1;
        } else if (distance < 4)
        {
            score = 2;
        } else if (distance > 4)
        {
            score = 3;
        }

        currentGame.score += score;
        scoreText.text = "Score: " + currentGame.score;
        if (score == 1) currentGame.freeThrows++;
        if (score == 2 || score == 3) currentGame.fieldGoals++;
    }

    /**
     * Resetira vrijednosti trenutne igre
     */
    private void ResetValues()
    {
        timeText.text = "Time: -";
        scoreText.text = "Score: 0";
        timer = gameLength;
        currentGame = null;
        isNearEnd = false;
        startStopGameButton.GetComponent<ButtonConfigHelper>().MainLabelText = "Start game";
        startStopGameButton.GetComponent<ButtonConfigHelper>().SetQuadIconByName("IconDone");
    }

    /**
     * Ažurira vrijeme igre te završava istu ako je došlo do kraja igre.
     * Metoda se pokre?e svaki frame.
     */
    private void UpdateGameStats()
    {
        if (currentGame != null) // ako je igra aktivna, ažuriraj timer
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                timeText.text = "Time: " + Math.Round(timer, 2).ToString();
                if (timer < 10f && !isNearEnd)
                {
                    isNearEnd = true;
                    Debug.Log("playing audio");
                    hoopObject.GetComponent<AudioSource>().Play();
                }
            }
            else
            {
                hoopObject.GetComponent<AudioSource>().Stop();
                // ažuriraj tablicu rezultata
                currentGame.number = ++gameCount;
                currentGame.gameTime = gameLength;
                highscore = highscore < currentGame.score ? currentGame.score : highscore;
                highScoreText.text = "Current highscore: " + highscore.ToString();

                AddToScoreboard(currentGame);
                ResetValues();
            }
        }
    }

    #endregion

    #region Objects

    /**
     * Objekt koji reprezentira jednu igru.
     * Sadrži statistiku za tablicu rezultata
     */
    private class Game
    {
        public int number;
        public int score;
        public int fieldGoals; //3 poena
        public int freeThrows; //1 poen
        public float gameTime;
    }
    #endregion
}
