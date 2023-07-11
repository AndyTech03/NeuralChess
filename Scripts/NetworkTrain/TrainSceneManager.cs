using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chess.Game;
using Chess;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static PlayerPrefsManger;

public class TrainSceneManager : MonoBehaviour
{
    public TrainSceneAnalizeManager AnalizeManager;
    public TrainSceneDuelManager DuelManager;
    public TrainSceneTrainManager TrainManager;
    public Camera MainCamera;
    public Canvas Canvas;

    public Button ExitButton;
    public Button ToggleButton;
    public GameObject LogPanel;
    public TMPro.TMP_Text LogText;

    public LanguagesManager LanguagesManager;

    public void Awake()
    {
        AnalizeManager.Canvas = DuelManager.Canvas = TrainManager.Canvas = Canvas;

        AnalizeManager.OnAnalizeFinish += AnalizeManager_OnAnalizeFinish;

        DuelManager.OnDuelsFinish += DuelManager_OnDuelFinish;
        DuelManager.LanguagesManager = LanguagesManager;

        TrainManager.OnLog += Log;
        TrainManager.OnTrainFinish += TrainManager_OnTrainFinish;
        TrainManager.LanguagesManager = LanguagesManager;

        DuelManager.OnLog += Log;
        ToggleButton.onClick.AddListener(ToggleLogPanel);
        ExitButton.onClick.AddListener(ExitToMenu);
    }
    private void Log(string text)
    {
        //Debug.Log(text);
        LogText.text =
            $"{text}\n" +
            $"{LogText.text}";
    }

    void ExitToMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }

    void ToggleLogPanel()
    {

        RectTransform rectTransform = (RectTransform)LogPanel.transform;

        bool isShowed = rectTransform.anchoredPosition.x == 250;

        float speed = 2;
        if (isShowed)
        {
            Vector3 targetPoint = new Vector3(rectTransform.sizeDelta.x / -2, 0);
            rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, targetPoint, speed);
        }
        else
        {
            Vector3 targetPoint = new Vector3(rectTransform.sizeDelta.x / 2, 0);
            rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, targetPoint, speed);
        }
    }

    public void Start()
    {
        LogAnalizeStart();
        AnalizeManager.StartGeneration(NeuralNetworks_Manager.GetAllNetworks_FromNetsDirectory());
    }

    void LogDuelStart()
    {
        string text;
        switch (LanguagesManager.CurentLanguage)
        {
            case LanguagesManager.Language.Russian:
                text = "Начался этап дуэлей!";
                break;
            case LanguagesManager.Language.English:
            default:
                text = "The stage of duels has begun!";
                break;
        }

        Log('\n' + text);
    }
    void LogTrainStart()
    {
        string text;
        switch (LanguagesManager.CurentLanguage)
        {
            case LanguagesManager.Language.Russian:
                text = "Начался этап обучения!";
                break;
            case LanguagesManager.Language.English:
            default:
                text = "The training phase has begun!";
                break;
        }

        Log('\n' + text);
    }
    void LogAnalizeStart()
    {
        string text;
        switch (LanguagesManager.CurentLanguage)
        {
            case LanguagesManager.Language.Russian:
                text = "Начался этап анализа!";
                break;
            case LanguagesManager.Language.English:
            default:
                text = "The analysis phase has begun!";
                break;
        }

        Log('\n' + text);
    }

    private void TrainManager_OnTrainFinish()
    {
        LogDuelStart();
        string keyName = DuelIteration_KeyName;
        TrainSceneDuelManager.DuelsConfig duelsConfig = new TrainSceneDuelManager.DuelsConfig()
        {
            IterationsCount = PlayerPrefs.GetInt(keyName, 3)
        };
        DuelManager.StartDuel(NeuralNetworks_Manager.GetAllNetworks_FromNetsDirectory(), duelsConfig);

    }

    private void DuelManager_OnDuelFinish()
    {
        LogAnalizeStart();
        AnalizeManager.StartGeneration(NeuralNetworks_Manager.GetAllNetworks_FromNetsDirectory());
    }

    private void AnalizeManager_OnAnalizeFinish(List<List<TrainSceneAnalizeManager.TrainData>> datasets)
    {
        LogTrainStart();
        TrainManager.StartTrain(NeuralNetworks_Manager.GetAllNetworks_FromNetsDirectory(), datasets);
    }
}
