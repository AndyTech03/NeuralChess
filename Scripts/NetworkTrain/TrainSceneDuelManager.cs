using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chess;
using Chess.Game;
using System.Linq;
using TMPro;
using static PlayerPrefsManger;

public class TrainSceneDuelManager : MonoBehaviour
{
    public event System.Action OnDuelsFinish;
    public event System.Action StartIteration;
    public TMP_Text LogText;

    public Canvas Canvas;
    public GameObject DuelField;
    public GameObject DuelBoardPrefab;
    public List<DuelBoardManager> DuelBoards;
    public ChessBoardsFactory BoardsFactory;

    public int MaxNetworks_AfterDuels = 16;
    public int TopListCount = 4;
    public int notEditingNetsDeviser = 3;
    public int destroyNetsCount = 2;

    public LanguagesManager LanguagesManager;

    List<NeuralNetwork> WhiteList;
    List<NeuralNetwork> BlackList;
    List<NeuralNetwork> QueueList;

    int curentIteration;
    int totalIterations;

    public event System.Action<string> OnLog;

    void CreateBoards(int networksCount)
    {
        DuelField = Instantiate(new GameObject(nameof(DuelField)), Canvas.transform);
        int boardsCount = networksCount / 2;

        int rowCount = Mathf.CeilToInt(Mathf.Sqrt(boardsCount)); 

        for (int i = 0; i < boardsCount; i++)
        {
            float x = i % rowCount * (ChessBoardManager.Width + 1);
            float y = i / rowCount * (ChessBoardManager.Height + 1);
            DuelBoardManager duelBoard = (DuelBoardManager)BoardsFactory.CreateBoard(DuelBoardPrefab, DuelField.transform, new Vector3(x, -y, 0));
            duelBoard.ChessBoard.TimerDuration = PlayerPrefs.GetInt(DuelMatchDuration_KeyName, 150);

            DuelBoards.Add(duelBoard);
            StartIteration += duelBoard.ChessBoard.NewGame;
            duelBoard.OnDuelFinish += DuelBoard_OnDuelFinish;
        }
    }

    void InitVariables()
    {
        DuelBoards = new List<DuelBoardManager>();
        WhiteList = new List<NeuralNetwork>();
        BlackList = new List<NeuralNetwork>();
        QueueList = new List<NeuralNetwork>();
    }

    void SetIterations(int iterationsCoutn)
    {
        curentIteration = 0;
        totalIterations = iterationsCoutn;
    }

    void CreateQueue(List<NeuralNetwork> networks)
    {
        List<NeuralNetwork> avaliableNets = new List<NeuralNetwork>();

        avaliableNets.AddRange(networks);

        for (int i = 0; i < networks.Count / 2; i++)
        {
            int index = Random.Range(0, avaliableNets.Count - 1);
            NeuralNetwork net = avaliableNets[index];
            WhiteList.Add(net);
            avaliableNets.RemoveAt(index);
        }

        for (int i = 0; i < networks.Count / 2; i++)
        {
            int index = Random.Range(0, avaliableNets.Count - 1);
            NeuralNetwork net = avaliableNets[index];
            BlackList.Add(net);
            avaliableNets.RemoveAt(index);
        }

        QueueList.AddRange(avaliableNets);
    }

    public class DuelsConfig
    {
        public int IterationsCount = 0;
    }

    public void StartDuel(List<NeuralNetwork> networks, DuelsConfig config)
    {
        InitVariables();
        CreateBoards(networks.Count);
        CreateQueue(networks);
        SetIterations(config.IterationsCount);

        Invoke(nameof(StartNewIteration), 0.1f);
    }

    private void DuelBoard_OnDuelFinish(GameManager.Result result, DuelBoardManager duelBoard)
    {
        int index = DuelBoards.IndexOf(duelBoard);

        WhiteList[index] = duelBoard.WhiteNetwork;
        BlackList[index] = duelBoard.BlackNetwork;

        for (int i = 0; i < DuelBoards.Count; i++)
        {
            if (DuelBoards[i].ChessBoard.gameResult == GameManager.Result.Playing)
                return;
        }

        LogTopList();
        SaveNetworks();

        if (curentIteration >= totalIterations)
        {
            Invoke(nameof(DuelsFinish), 0.1f);
        }
        else
        {
            Invoke(nameof(StartNewIteration), 0.1f);
        }
    }

    List<NeuralNetwork> GetSortedNetworks()
    {
        List<NeuralNetwork> networks = new List<NeuralNetwork>();
        networks.AddRange(WhiteList.ToArray());
        networks.AddRange(BlackList.ToArray());
        networks.AddRange(QueueList.ToArray());

        networks.Sort(delegate (NeuralNetwork n1, NeuralNetwork n2) { return n2.Fitnes.CompareTo(n1.Fitnes); });
        return networks;
    }

    void LogTopList()
    {
        List<NeuralNetwork> networks = GetSortedNetworks();
        int topListCount = networks.Count < 5 ? networks.Count : 5;
        string text;
        switch (LanguagesManager.CurentLanguage)
        {
            case LanguagesManager.Language.Russian:
                {
                    text =
                        $"\n" +
                        $"Раунд дуэли: {curentIteration}/{totalIterations}.\n" +
                        $"Список лучших:";
                    for (int i = 0; i < topListCount; i++)
                    {
                        text += $"\n{i + 1} | {networks[i]} : {networks[i].Fitnes}";
                    }
                    break;
                }
            case LanguagesManager.Language.English:
            default:
                {
                    text =
                        $"\n" +
                        $"Duel iteration: {curentIteration}/{totalIterations}.\n" +
                        $"Top list:";
                    for (int i = 0; i < topListCount; i++)
                    {
                        text += $"\n{i + 1} | {networks[i]} : {networks[i].Fitnes}";
                    }
                    break;
                }
        }
        Log(text);
    }

    private void Log(string text)
    {
        OnLog?.Invoke(text);
    }

    void StartNewIteration()
    {
        MoveBoardsLists(ref WhiteList, ref BlackList, ref QueueList);

        for (int i = 0; i< DuelBoards.Count; i++)
        {
            DuelBoards[i].WhiteNetwork = WhiteList[i];
            DuelBoards[i].BlackNetwork = BlackList[i];
        }
        StartIteration?.Invoke();
        curentIteration++;
    }

    void LogNetworkRemoving(NeuralNetwork removingNetwork)
    {
        switch (LanguagesManager.CurentLanguage)
        {
            case LanguagesManager.Language.Russian:
                Log($"Сеть {removingNetwork} удалена!");
                break;
            case LanguagesManager.Language.English:
            default:
                Log($"Network {removingNetwork} removed!");
                break;
        }
        
    }

    void LogNewNetwork(NeuralNetwork newNet, NeuralNetwork old1 = null, NeuralNetwork old2 = null)
    {
        if (old1 is null && old2 is null)
        {
            switch (LanguagesManager.CurentLanguage)
            {
                case LanguagesManager.Language.Russian:
                    Log($"Добаленна новая сеть {newNet}.");
                    break;
                case LanguagesManager.Language.English:
                default:
                    Log($"Added new network {newNet}.");
                    break;
            }
        }
        if (old1 is NeuralNetwork && old2 is null)
        {
            switch (LanguagesManager.CurentLanguage)
            {
                case LanguagesManager.Language.Russian:
                    Log($"Сеть {old1} удалена и добавлена новая сеть - {newNet}.");
                    break;
                case LanguagesManager.Language.English:
                default:
                    Log($"The network {old1} has been removed and {newNet} has been added.");
                    break;
            }
        }
        else if (old1 is NeuralNetwork && old2 is NeuralNetwork)
        {
            switch (LanguagesManager.CurentLanguage)
            {
                case LanguagesManager.Language.Russian:
                    Log($"Новая сеть: {old1} + {old2} => {newNet}.");
                    break;
                case LanguagesManager.Language.English:
                default:
                    Log($"New network: {old1} + {old2} => {newNet}.");
                    break;
            }
        }
    }

    void NetworksFix_SelectBest_RemoveWorst()
    {
        List<NeuralNetwork> networks = GetSortedNetworks();

        List<NeuralNetwork> TopList = new List<NeuralNetwork>();
        for (int i = 0; i < TopListCount; i++)
        {
            TopList.Add(networks[i]);
        }
        for (int i = networks.Count - 1; i > MaxNetworks_AfterDuels - 1; i--)
        {
            NeuralNetwork removingNetwork = networks[i];
            networks.RemoveAt(i);
            removingNetwork.Delete();
            LogNetworkRemoving(removingNetwork);
        }

        int netsCount = networks.Count;
        int notEditingNetsCount = TopListCount + (netsCount - TopListCount) / notEditingNetsDeviser;
        int resetNetsCount = netsCount - notEditingNetsCount;

        for (int i = netsCount - destroyNetsCount - 1; i > netsCount - resetNetsCount - 1; i--)
        {
            NeuralNetwork resetingNet = networks[i];
            NeuralNetwork top = TopList.Find(net => net.LayersStructure.SequenceEqual(resetingNet.LayersStructure) && net.ActivationsStructure.SequenceEqual(resetingNet.ActivationsStructure)) ?? resetingNet;
            NeuralNetwork newNet = resetingNet + top;
            LogNewNetwork(newNet, top, resetingNet);

            resetingNet.Delete();
            newNet.Save();
        }

        for (int i = netsCount - 1; i > netsCount - destroyNetsCount - 1; i--)
        {
            NeuralNetwork destroingNet = networks[i];

            GenerateLayers(out string[] layersActivations, out int[] layersStruct);

            NeuralNetwork newNet = new NeuralNetwork(layersStruct, layersActivations, NeuralNetwork.GenerateName());
            LogNewNetwork(newNet, destroingNet);

            destroingNet.Delete();
            newNet.Save();
        }

        for (int i = netsCount - 1; i < MaxNetworks_AfterDuels; i++)
        {
            GenerateLayers(out string[] layersActivations, out int[] layersStruct);

            NeuralNetwork newNet = new NeuralNetwork(layersStruct, layersActivations, NeuralNetwork.GenerateName());
            LogNewNetwork(newNet);

            newNet.Save();
        }

        Log("");
    }

    void ClearMemory()
    {
        for (int i = 0; i < DuelBoards.Count; i++)
        {
            StartIteration -= DuelBoards[i].ChessBoard.NewGame;
        }
        BoardsFactory.Clear();
        Destroy(DuelField);

        WhiteList = null;
        BlackList = null;
        QueueList = null;
        DuelBoards = null;
    }

    void DuelsFinish()
    {
        NetworksFix_SelectBest_RemoveWorst();
        ClearMemory();
        OnDuelsFinish?.Invoke();

    }

    readonly int[] inputsSizes = NeuralNetworks_Manager.InputSizes;
    readonly int[] hidensSizes = NeuralNetworks_Manager.HidenSizes;
    readonly int[] outputSizes = NeuralNetworks_Manager.OutputSizes;
    readonly string[] activationNames = NeuralNetworks_Manager.ActivationsNames;

    public void GenerateLayers(out string[] layersActivations, out int[] layersStruct)
    {
        int deep = Random.Range(3, 6);

        layersActivations = new string[deep-1];
        layersStruct = new int[deep];

        for (int i = 0; i<deep; i++)
        {
            int index;
            if (i == 0)
            {
                index = Random.Range(0, inputsSizes.Length);
                layersStruct[i] = inputsSizes[index];

                index = Random.Range(0, activationNames.Length);
                layersActivations[i] = activationNames[index];
            }
            else if (i == deep - 1)
            {
                index = Random.Range(0, outputSizes.Length);
                layersStruct[i] = outputSizes[index];
            }
            else
            {
                index = Random.Range(0, hidensSizes.Length);
                layersStruct[i] = hidensSizes[index];

                index = Random.Range(0, activationNames.Length);
                layersActivations[i] = activationNames[index];
            }
        }
        
    }

    void SaveNetworks()
    {
        List<NeuralNetwork> networks = new List<NeuralNetwork>();
        networks.AddRange(WhiteList.ToArray());
        networks.AddRange(BlackList.ToArray());
        networks.AddRange(QueueList.ToArray());

        networks.ForEach(delegate (NeuralNetwork network) { network.Save(owerride:true); });
    }

    void MoveBoardsLists(ref List<NeuralNetwork> whiteList, ref List<NeuralNetwork> blackList, ref List<NeuralNetwork> queueList)
    {
        NeuralNetwork net = whiteList[0];
        queueList.Add(net);
        whiteList.Remove(net);

        net = blackList.Last();
        queueList.Add(net);
        blackList.Remove(net);

        net = queueList[0];
        blackList.Insert(0, net);
        queueList.Remove(net);

        net = queueList[0];
        whiteList.Add(net);
        queueList.Remove(net);
    }
}
