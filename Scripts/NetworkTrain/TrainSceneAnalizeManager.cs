using Chess;
using Chess.Game;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static PlayerPrefsManger;

public class TrainSceneAnalizeManager : MonoBehaviour
{
    public event System.Action<List<List<TrainData>>> OnAnalizeFinish;

    public Canvas Canvas;
    public GameObject TrainBoardPrefab;
    GameObject DuelField;
    public ChessBoardsFactory BoardsFactory;
    List<TrainBoardManager> TrainBoards;

    List<List<TrainData>> trainDatasets;
    int finishedCount;
    int totalCount;
    bool Writing;

    public class TrainData
    {
        public string Fen;
        public int TrueEvaluation;
        public int NetworkError;
    }
    class TrainDataComparer : IEqualityComparer<TrainData>
    {
        public bool Equals(TrainData x, TrainData y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Fen.Equals(y.Fen);
        }

        public int GetHashCode(TrainData obj)
        {
            if (obj is null) return 0;

            return obj.GetHashCode();
        }
    }

    public void Start()
    {
    }

    public void Update()
    {

    }

    void InitVariables()
    {
        TrainBoards = new List<TrainBoardManager>();
        trainDatasets = new List<List<TrainData>>();
        Writing = true;
    }

    void CreateBoards(List<NeuralNetwork> networks)
    {
        finishedCount = 0;
        totalCount = networks.Count;
        DuelField = Instantiate(new GameObject(nameof(DuelField)), Canvas.transform);
        int boardsCount = networks.Count;

        int rowCount = Mathf.CeilToInt(Mathf.Sqrt(boardsCount));

        for (int i = 0; i < boardsCount; i++)
        {
            float x = i % rowCount * (ChessBoardManager.Width + 1);
            float y = i / rowCount * (ChessBoardManager.Height + 1);
            TrainBoardManager trainBoard = (TrainBoardManager)BoardsFactory.CreateBoard(TrainBoardPrefab, DuelField.transform, new Vector3(x, -y, 0));
            trainBoard.TrainingNetwork = networks[i];
            trainBoard.TraintingNetEvaluation.ID = i;
            TrainBoards.Add(trainBoard);
            trainDatasets.Add(new List<TrainData>());

            trainBoard.OnTrainFinish += OnTrainFinish;
            trainBoard.TraintingNetEvaluation.OnEvaluateWithTrainer += OnEvaluateWithTrainer;

            string keyName = GenerationMatchDuration_KeyName;
            trainBoard.ChessBoard.TimerDuration = PlayerPrefs.GetInt(keyName, 30);

            trainBoard.ChessBoard.NewGame();
        }
    }

    private void OnEvaluateWithTrainer(int netID, string fen, int networkEvaluate, int trainerEvaluate)
    {
        if (Writing)
            trainDatasets[netID].Add(new TrainData() { Fen = fen, TrueEvaluation = trainerEvaluate, NetworkError = Mathf.Abs(networkEvaluate - trainerEvaluate) });
    }

    private void OnTrainFinish(GameManager.Result result, TrainBoardManager board)
    {
        finishedCount++;

        if (finishedCount >= totalCount)
        {
            HideUI();
            Writing = false;
            Invoke(nameof(EndStage), 0.5f);
        }
    }

    void EndStage()
    {
        List<List<TrainData>> datasets = GetDatasets();
        ClearMemory();
        OnAnalizeFinish?.Invoke(datasets);
    }
    void HideUI()
    {
        for (int i =0; i<TrainBoards.Count; i++)
        {
            Destroy(TrainBoards[i]);
        }
        Destroy(DuelField);
    }
    void ClearMemory()
    {
        for (int i =0;i<TrainBoards.Count; i++)
        {
            TrainBoardManager trainBoard = TrainBoards[i];
            trainBoard.OnTrainFinish -= OnTrainFinish;
            trainBoard.TraintingNetEvaluation.OnEvaluateWithTrainer -= OnEvaluateWithTrainer;
        }
        TrainBoards.Clear();
        trainDatasets.Clear();
        BoardsFactory.Clear();
        TrainBoards = null;
        trainDatasets = null;
    }


    List<List<TrainData>> GetDatasets()
    {
        List<List<TrainData>> datasets = new List<List<TrainData>>();
        for (int i = 0; i<trainDatasets.Count; i++)
        {
            List<TrainData> data = new List<TrainData>();
            data.AddRange(trainDatasets[i]);
            data = data.Distinct(new TrainDataComparer()).ToList();
            data.Sort(delegate (TrainData d1, TrainData d2) { return d2.NetworkError.CompareTo(d1.NetworkError); });

            int datsetSize = PlayerPrefs.GetInt(DatasetSize_KeyName, 1000);
            int count = data.Count < datsetSize ? data.Count : datsetSize;
            datasets.Add(new List<TrainData>());

            for (int j =0; j<count; j++)
            {
                datasets[i].Add(data[j]);
            }
        }
        return datasets;
    }

    public void StartGeneration(List<NeuralNetwork> networks)
    {
        InitVariables();
        CreateBoards(networks);
    }
}
