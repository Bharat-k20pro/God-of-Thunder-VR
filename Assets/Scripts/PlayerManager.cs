using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    public static float playerHealth, currentScore, level;
    public Image healthImg;
    public TMP_Text scoreText, highScoreText, levelText;
    public GameObject gameON, gameOFF, enemies, l1, l2, l3, portal, portal2, plane, celebrate;
    public Material sb1, gr1;
    // Start is called before the first frame update
    void Start()
    {
        highScoreText.text = "High Score = " + PlayerPrefs.GetFloat("HighScore").ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerHealth <= 0)
        {
            GameOver();
        }
        healthImg.fillAmount = playerHealth;
        scoreText.text = currentScore.ToString();
        levelText.text = "Level=" + level.ToString();
        if (currentScore == 200)
        {
            l1.SetActive(false);
            portal.SetActive(true);
            currentScore += 100;
        }
        if (currentScore == 700)
        {
            l2.SetActive(false);
            portal2.SetActive(true);
            currentScore += 100;
        }
        if (currentScore == 1300)
        {
            celebrate.SetActive(true);
            l3.SetActive(false);
            currentScore += 100;
        }
    }

    public void GameStart()
    {
        gameON.SetActive(true);
        gameOFF.SetActive(false);
        playerHealth = 1;
        level = 1;
        currentScore = 0;
        scoreText.text = "0";
    }

    public void GameOver()
    {
        if (currentScore > PlayerPrefs.GetFloat("HighScore"))
        {
            PlayerPrefs.SetFloat("HighScore", currentScore);
            highScoreText.text = "High Score = " + PlayerPrefs.GetFloat("HighScore").ToString();
        }

        foreach (Transform child in enemies.transform)
        {
            child.gameObject.GetComponent<EnemyController>().Death();
        }
        l1.SetActive(true);
        l2.SetActive(false);
        l3.SetActive(false);
        portal.SetActive(false);
        portal2.SetActive(false);
        celebrate.SetActive(false);

        RenderSettings.skybox = sb1;
        plane.GetComponent<Renderer>().material = gr1;

        gameON.SetActive(false);
        gameOFF.SetActive(true);
    }
}
