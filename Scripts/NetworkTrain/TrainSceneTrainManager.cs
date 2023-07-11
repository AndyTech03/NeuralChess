using Chess;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlayerPrefsManger;

public class TrainSceneTrainManager : MonoBehaviour
{
    public event System.Action OnTrainFinish;
    public event System.Action<string> OnLog;

    public Canvas Canvas;
    public TMP_Text DebugText;
    public LanguagesManager LanguagesManager;

    List<NeuralNetwork> Networks;
    List<List<Board>> Boards_Lists;
    List<List<int>> Evals_Lists;
    List<Thread> Threads;

    List<float> ErrorsSumm;
    List<int> ErrorsCount;
    List<int> Iterations;
    public bool[] IsComplited;
    public bool Training;
    int iterations;
    int time;

    public void Start()
    {

    }
    public void StartTrain(List<NeuralNetwork> networks, List<List<TrainSceneAnalizeManager.TrainData>> datasets)
    {
        InitVariables();
        SetDataset(datasets);
        InitTrainThreads(networks);
        StartTrainingProcess();

        ShowUI();
    }
    void ShowUI()
    {
        DebugText.gameObject.SetActive(true);
    }

    void StartTrainingProcess()
    {
        for (int i = 0; i < Networks.Count; i++)
        {
            Threads[i].Start(i);
        }
        Training = true;
        time = 0;
    }

    void InitTrainThreads(List<NeuralNetwork> networks)
    {
        IsComplited = new bool[networks.Count];
        Networks = networks;
        for (int i = 0; i < Networks.Count; i++)
        {
            ErrorsSumm.Add(0);
            ErrorsCount.Add(0);
            Iterations.Add(0);
            IsComplited[i] = false;
            Threads.Add(new Thread(new ParameterizedThreadStart(TrainNet)));
        }
    }

    void InitVariables()
    {
        Boards_Lists = new List<List<Board>>();
        Evals_Lists = new List<List<int>>();
        Threads = new List<Thread>();
        ErrorsSumm = new List<float>();
        ErrorsCount = new List<int>();
        Iterations = new List<int>();
        string keyName = RepiatsCount_KeyName;
        iterations = PlayerPrefs.GetInt(keyName, 3);
    }

    void SetDataset(List<List<TrainSceneAnalizeManager.TrainData>> datasets)
    {
        for (int i = 0; i < datasets.Count; i++)
        {
            List<TrainSceneAnalizeManager.TrainData> data = datasets[i];
            Boards_Lists.Add(new List<Board>());
            Evals_Lists.Add(new List<int>());
            for (int j=0; j<data.Count; j++)
            {
                Board board = new Board();
                board.LoadPosition(data[j].Fen);
                Boards_Lists[i].Add(board);

                Evals_Lists[i].Add(data[j].TrueEvaluation);
            }
        }
    }

    void TrainNet(object Index)
    {
        int index = (int)Index;
        List<Board> boards = Boards_Lists[index];
        List<int> evals = Evals_Lists[index];
        int iterationsCount = 0;
        for (int i = 0; i < iterations; i++)
        {
            Iterations[index] = i;
            for (int j = 0; j < boards.Count; j++)
            {
                float[] inputs;
                if (Networks[index].LayersStructure[0] == 64)
                    inputs = NetWorkEvaluation.OneBoard(boards[j].Square);
                else
                    inputs = NetWorkEvaluation.TwoBoards(boards[j].Square);

                float[] target = NetWorkEvaluation.ValidateTarget(evals[j]);

                Networks[index].BackPropagate(inputs, target);

                float error = Mathf.Abs(Networks[index].FeedForward(inputs)[0] - target[0]);
                iterationsCount++;
                ErrorsCount[index] = iterationsCount;
                ErrorsSumm[index] += error;
            }
        }

        IsComplited[index] = true;
    }

    void SaveNetworks()
    {
        for (int i = 0; i < Networks.Count; i++)
        {
            Networks[i].Save(owerride:true);
        }
    }

    class SortedFloat
    {
        public float Value;
        public int Index;
        public SortedFloat(float value, int index)
        {
            Value = value;
            Index = index;
        }
    }

    public void FixedUpdate()
    {
        if (Training)
        {
            time++;
            if (time % 60 * 30 == 60 * 30 - 1)
            {
                SaveNetworks();
                time = 0;
            }
        }
    }

    public void Update()
    {
        if (Training)
        {
            UpdateUI();

            for (int i = 0; i < IsComplited.Length; i++)
            {
                if (!IsComplited[i])
                {
                    return;
                }
                else
                {
                    Threads[i].Abort();
                }
            }

            LogTopList();
            SaveNetworks();
            ClearMemory();
            HideUI();

            OnTrainFinish?.Invoke();
        }
    }

    void LogTopList()
    {
        List<SortedFloat> avarageErrors = SortList(5);
        
        string text;

        switch (LanguagesManager.CurentLanguage)
        {
            case LanguagesManager.Language.Russian:
                text = "Список лучших:";
                break;
            case LanguagesManager.Language.English:
            default:
                text = "Top list:";
                break;
        }

        for (int i = 0; i < avarageErrors.Count; i++)
        {
            int index = avarageErrors[i].Index;
            float avarageError = avarageErrors[i].Value;
            text += $"\n{i+1} | {avarageError:f5} | {Networks[index]}";
        }
        OnLog?.Invoke('\n' + text);
    }

    void UpdateUI()
    {
        List<SortedFloat> avarageErrors = SortList(Networks.Count);
        string text = "";

        for (int i = 0; i < avarageErrors.Count; i++)
        {
            int index = avarageErrors[i].Index;
            float avarageError = avarageErrors[i].Value;
            text += $"{Iterations[index] + 1:000} | {avarageError:f5} | {Networks[index]}\n";
        }
        DebugText.text = text;
    }

    List<SortedFloat> SortList(int countOfMembers)
    {
        List<SortedFloat> avarageErrors = new List<SortedFloat>();
        for (int i = 0; i < countOfMembers; i++)
        {
            float avarageError;
            avarageError = ErrorsSumm[i] / ErrorsCount[i];
            avarageErrors.Add(new SortedFloat(avarageError, i));

        }
        avarageErrors.Sort(delegate (SortedFloat sf1, SortedFloat sf2) { return sf1.Value.CompareTo(sf2.Value); });

        return avarageErrors;
    }

    void HideUI()
    {
        DebugText.gameObject.SetActive(false);
    }

    void ClearMemory()
    {
        Training = false;
        Threads = null;
        ErrorsSumm = null;
        ErrorsCount = null;
        Iterations = null;
        IsComplited = null;
        Networks = null;
        Boards_Lists = null;
        Evals_Lists = null;
        DebugText.text = "";
    }
        
}
