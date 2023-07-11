using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using static PlayerPrefsManger;

namespace Chess
{
    public static class NeuralNetworks_Manager
    {
        #region Fast create assets

        public static int[] InputSizes { get => new int[] { 64, 128 }; }
        public static int[] HidenSizes { get => new int[] { 128, 64, 32, 16, 8, 4, 2 }; }
        public static int[] OutputSizes { get => new int[] { 1 }; }
        public static string[] ActivationsNames { get => new string[] { "tanh", "leakyrelu", "arctanh", "invsquare", "sin", "sinc" }; }

        #endregion
        #region File system

        public static string NetworksDirPath 
        { 
            get => PlayerPrefs.GetString(NetworksPath_KeyName, "Neural Networks"); 
            set { PlayerPrefs.SetString(NetworksPath_KeyName, value); } 
        }

        public static string FileName_Prefix
        {
            get => PlayerPrefs.GetString(NetworkFileNamePrefix_KeyName, "Net ");
            set { PlayerPrefs.SetString(NetworkFileNamePrefix_KeyName, value); }
        }

        public static string FileExtention
        {
            get => PlayerPrefs.GetString(NetworkFileExtention_KeyName, "json");
            set { PlayerPrefs.SetString(NetworkFileExtention_KeyName, value); }
        }

        public static string GetNetworkFileName(NeuralNetwork net)
        {
            return $"{NetworksDirPath}//{FileName_Prefix}{net.Network_Name}{(net.NetworkCopyIndex == 0 ? "" : $" ({net.NetworkCopyIndex})")}.{FileExtention}";
        }

        #endregion

        public static List<NeuralNetwork> GetAllNetworks_FromNetsDirectory()
        {
            List<NeuralNetwork> result = new List<NeuralNetwork>();
            if (Directory.Exists(NetworksDirPath))
            {
                string[] filesPath = Directory.GetFiles(NetworksDirPath, $"{FileName_Prefix}*.{FileExtention}");
                for (int i = 0; i<filesPath.Length; i++)
                {
                    NeuralNetwork network = NeuralNetwork.LoadFromFile(filesPath[i]);
                    result.Add(network);
                }
            }
            else
            {
                Directory.CreateDirectory(NetworksDirPath);
                Debug.LogWarning("Network directory is not exist! Created empty.");
            }

            return result;
        }
    }

    public class SerializedNetwork
    {
        #region Core

        public int[] LayersStructure;
        public string[] ActivationsStructure;
        public float[] SerializedWeights;

        #endregion
        #region Additional

        public int Wins;
        public int Draws;
        public int Losses;

        public string Network_Name;

        public string Parent1_Name;
        public string Parent2_Name;
        public int Network_Generation;

        public int NetworkCopyIndex;

        public SerializedNetwork(NeuralNetwork neuralNetwork)
        {
            LayersStructure = neuralNetwork.LayersStructure;
            ActivationsStructure = neuralNetwork.ActivationsStructure;

            Wins = neuralNetwork.Wins;
            Draws = neuralNetwork.Draws;
            Losses = neuralNetwork.Losses;

            Network_Name = neuralNetwork.Network_Name;

            Parent1_Name = neuralNetwork.Parent1_Name;
            Parent2_Name = neuralNetwork.Parent2_Name;

            Network_Generation = neuralNetwork.Network_Generation;
            NetworkCopyIndex = neuralNetwork.NetworkCopyIndex;

            List<float> serializingWeights = new List<float>();

            for (int i =0; i<neuralNetwork.Weights.Length; i++)
            {
                for (int j = 0; j<neuralNetwork.Weights[i].Length; j++)
                {
                    for (int k = 0; k<neuralNetwork.Weights[i][j].Length; k++)
                    {
                        serializingWeights.Add(neuralNetwork.Weights[i][j][k]);
                    }
                }
            }

            SerializedWeights = serializingWeights.ToArray();
        }

        public NeuralNetwork UnserializeNetwork()
        {
            return new NeuralNetwork(this);
        }

        #endregion
    }

    public class NeuralNetwork
    {
        #region Variables
        #region Core

        public int[] LayersStructure;
        public string[] ActivationsStructure;
        //fundamental 
        private readonly int[] layers;//layers
        private float[][] neurons;//neurons
        private float[][][] weights;//weights
        private readonly int[] activations;//layers

        public float[][][] Weights { get => weights; }

        //genetic
        public float fitness = 0;//fitness

        //backprop
        public const float learningRate = 0.003f;//learning rate
        public float cost = 0;

        #endregion
        #region Additional

        public int Wins;
        public int Draws;
        public int Losses;

        public float Fitnes;

        public string Network_Name { get; set; }
        public int NetworkCopyIndex;

        public string Parent1_Name;
        public string Parent2_Name;

        public int Network_Generation;

        #endregion
        #endregion

        #region Init

        public NeuralNetwork(SerializedNetwork serializedNetwork)
        {
            LayersStructure = serializedNetwork.LayersStructure;
            ActivationsStructure = serializedNetwork.ActivationsStructure;

            Wins = serializedNetwork.Wins;
            Losses = serializedNetwork.Losses;
            Draws = serializedNetwork.Draws;

            Network_Name = serializedNetwork.Network_Name;

            Parent1_Name = serializedNetwork.Parent1_Name;
            Parent2_Name = serializedNetwork.Parent2_Name;

            Network_Generation = serializedNetwork.Network_Generation;
            NetworkCopyIndex = serializedNetwork.NetworkCopyIndex;

            layers = new int[LayersStructure.Length];
            activations = new int[ActivationsStructure.Length];

            InitNetwork(serializedNetwork.SerializedWeights);
        }

        public NeuralNetwork(int[] layersStructure, string[] activationsStructure, string name)
        {
            LayersStructure = layersStructure;
            ActivationsStructure = activationsStructure;

            Network_Name = name;

            NetworkCopyIndex = 0;
            Network_Generation = 0;

            Wins = 0;
            Draws = 0;
            Losses = 0;

            layers = new int[layersStructure.Length];
            activations = new int[activationsStructure.Length];

            if (activations.Length != layers.Length - 1)
                throw new Exception("Incorrect arrays length!");
            else
                InitNetwork();
        }

        /// <summary>
        /// Initialize network arrays.
        /// </summary>
        private void InitNetwork(float[] serializedWeights = null)
        {
            InitLayers();
            InitActivations();
            InitEmptyNeurons();
            InitWeights(serializedWeights);
        }

        private void InitLayers()
        {
            for (int i = 0; i < LayersStructure.Length; i++)
            {
                layers[i] = LayersStructure[i];
            }
        }

        private void InitActivations()
        {
            for (int i = 0; i < ActivationsStructure.Length - 1; i++)
            {
                string action = ActivationsStructure[i];
                switch (action)
                {
                    case "tanh":
                        activations[i] = 1;
                        break;
                    case "leakyrelu":
                        activations[i] = 2;
                        break;
                    case "arctanh":
                        activations[i] = 3;
                        break;
                    case "invsquare":
                        activations[i] = 4;
                        break;
                    case "sin":
                        activations[i] = 5;
                        break;
                    case "sinc":
                        activations[i] = 6;
                        break;
                    default:
                        activations[i] = 1;
                        break;
                }
            }
        }

        /// <summary>
        /// Create empty storage array for the neurons in the network.
        /// </summary>
        private void InitEmptyNeurons()
        {
            List<float[]> neuronsList = new List<float[]>();
            for (int i = 0; i < layers.Length; i++)
            {
                neuronsList.Add(new float[layers[i]]);
            }
            neurons = neuronsList.ToArray();
        }

        /// <summary>
        /// Initializes random array for the weights being held in the network.
        /// </summary>
        private void InitWeights(float[] data = null)
        {
            int index = 0;
            List<float[][]> weightsList = new List<float[][]>();
            for (int i = 1; i < layers.Length; i++)
            {
                List<float[]> layerWeightsList = new List<float[]>();
                int neuronsInPreviousLayer = layers[i - 1];
                for (int j = 0; j < layers[i]; j++)
                {
                    float[] neuronWeights = new float[neuronsInPreviousLayer];
                    for (int k = 0; k < neuronsInPreviousLayer; k++)
                    {
                        if (data is null)
                        {
                            neuronWeights[k] = UnityEngine.Random.Range(-0.5f, 0.5f);
                        }
                        else
                        {
                            neuronWeights[k] = data[index];
                            index++;
                        }
                    }
                    layerWeightsList.Add(neuronWeights);
                }
                weightsList.Add(layerWeightsList.ToArray());
            }
            weights = weightsList.ToArray();
        }

        #endregion
        #region Logick

        public float[] FeedForward(float[] inputs)//feed forward, inputs >==> outputs.
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                neurons[0][i] = inputs[i] != 0 ? inputs[i] : 0.01f;
            }
            for (int i = 1; i < layers.Length; i++)
            {
                int layer = i - 1;
                for (int j = 0; j < layers[i]; j++)
                {
                    float value = 0f;
                    for (int k = 0; k < layers[i - 1]; k++)
                    {
                        value += weights[i - 1][j][k] * neurons[i - 1][k];
                    }
                    float input = value;
                    float output = Activate(input, layer);
                    neurons[i][j] = output;
                }
            }
            return neurons[layers.Length - 1];
        }

        public float Activate(float value, int layer)//all activation functions
        {
            switch (activations[layer])
            {
                case 1:
                    return Tanh(value);
                case 2:
                    return Leakyrelu(value);
                case 3:
                    return ArcTanh(value);
                case 4:
                    return InvSquare(value);
                case 5:
                    return Sin(value);
                case 6:
                    return SinC(value);

                default:
                    return Tanh(value);
            }
        }

        public float ActivateDer(float value, int layer)//all activation function derivatives
        {
            switch (activations[layer])
            {
                case 1:
                    return TanhDer(value);
                case 2:
                    return LeakyreluDer(value);
                case 3:
                    return ArcTanhDer(value);
                case 4:
                    return InvSquareDer(value);
                case 5:
                    return SinDer(value);
                case 6:
                    return SinCDer(value);

                default:
                    return TanhDer(value);
            }
        }

        #region Activations Func

        public float Tanh(float x)
        {
            float result = (float)Math.Tanh(x);
            return result;
        }
        public float Leakyrelu(float x)
        {
            return (0 >= x) ? 0.01f * x : x;
        }
        public float ArcTanh(float x)
        {
            return (float)Mathf.Atan(x);
        }
        public float InvSquare(float x)
        {
            return x / Mathf.Sqrt(1.0f + x * x);
        }
        public float Sin(float x)
        {
            return Mathf.Sin(x);
        }
        public float SinC(float x)
        {
            return x == 0 ? 1.0f : Mathf.Sin(x) / x;
        }

        #endregion
        #region Activations Der func

        public float TanhDer(float x)
        {
            return 1 - (x * x);
        }
        public float LeakyreluDer(float x)
        {
            return (0 >= x) ? 0.01f : 1;
        }
        public float ArcTanhDer(float x)
        {
            return 1.0f / (x * x + 1.0f);
        }
        public float InvSquareDer(float x)
        {
            return Mathf.Pow(1.0f / Mathf.Sqrt(1.0f + x * x), 3);
        }
        public float SinDer(float x)
        {
            return Mathf.Cos(x);
        }
        public float SinCDer(float x)
        {
            return x == 0 ? 0.0f : (Mathf.Cos(x) / x) - (Mathf.Sin(x) / (x * x));
        }

        #endregion
        #endregion
        #region Learning

        //Genetic implementations down onwards until save.
        public void Mutate(int high, float val)//used as a simple mutation function for any genetic implementations.
        {
            for (int i = 0; i < weights.Length; i++)
            {
                for (int j = 0; j < weights[i].Length; j++)
                {
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        weights[i][j][k] = (UnityEngine.Random.Range(0f, 100) <= high) ? weights[i][j][k] += UnityEngine.Random.Range(-val, val) : weights[i][j][k];
                    }
                }
            }
        }
        public void BackPropagate(float[] inputs, float[] expected)//backpropogation;
        {
            float[] output = FeedForward(inputs);//runs feed forward to ensure neurons are populated correctly

            cost = 0;
            for (int i = 0; i < output.Length; i++) cost += (float)Math.Pow(output[i] - expected[i], 2);//calculated cost of network
            cost /= 2;//this value is not used in calculions, rather used to identify the performance of the network

            float[][] gamma;
            List<float[]> gammaList = new List<float[]>();
            for (int i = 0; i < layers.Length; i++)
            {
                gammaList.Add(new float[layers[i]]);
            }
            gamma = gammaList.ToArray();

            int layer = layers.Length - 2;
            for (int i = 0; i < output.Length; i++) 
                gamma[layers.Length - 1][i] = (output[i] - expected[i]) * ActivateDer(output[i], layer);

            for (int i = 0; i < layers[layers.Length - 1]; i++)
            {
                for (int j = 0; j < layers[layers.Length - 2]; j++)
                {
                    weights[layers.Length - 2][i][j] -= gamma[layers.Length - 1][i] * neurons[layers.Length - 2][j] * learningRate; 
                }
            }

            for (int i = layers.Length - 2; i > 0; i--)
            {
                layer = i - 1;
                for (int j = 0; j < layers[i]; j++)
                {
                    gamma[i][j] = 0;
                    for (int k = 0; k < gamma[i + 1].Length; k++)
                    {
                        gamma[i][j] += gamma[i + 1][k] * weights[i][k][j];
                    }
                    gamma[i][j] *= ActivateDer(neurons[i][j], layer);
                }
                for (int j = 0; j < layers[i]; j++)
                {
                    for (int k = 0; k < layers[i - 1]; k++)
                    {
                        weights[i - 1][j][k] -= gamma[i][j] * neurons[i - 1][k] * learningRate;
                    }
                }
            }
        }

        public static string GenerateName()
        {
            List<string> FirstNames = new List<string>() { "Test", "Betta", "Alpha", "Sky", "Chess", "Neural", "Perfect", "Unstoppable" };
            List<string> SecondNames = new List<string>() { "Net", "Champion", "Master",  "Titan", "Grandmaster", "Calculator", "Predictor" };
            int firstIndex = UnityEngine.Random.Range(0, FirstNames.Count);
            int secondIndex = UnityEngine.Random.Range(0, SecondNames.Count);

            string firstName = FirstNames[firstIndex];
            string secondName = SecondNames[secondIndex];

            return $"{firstName} {secondName}";
        }

        public static NeuralNetwork operator + (NeuralNetwork parent1, NeuralNetwork parent2)
        {
            NeuralNetwork result = new NeuralNetwork(parent1.LayersStructure, parent1.ActivationsStructure, GenerateName());

            result.Parent1_Name = parent1.Network_Name;
            result.Parent2_Name = parent2.Network_Name;
            result.Network_Generation = parent1.Network_Generation + 1;

            for (int i = 0; i < result.weights.Length; i++)
            {
                for (int j = 0; j < result.weights[i].Length; j++)
                {
                    for (int k = 0; k < result.weights[i][j].Length; k++)
                    {
                        int delta = UnityEngine.Random.Range(0, 100);
                        if (delta < 50)
                            result.weights[i][j][k] = parent1.weights[i][j][k];
                        else
                            result.weights[i][j][k] = parent2.weights[i][j][k];
                    }
                }
            }

            result.Mutate(10, 0.3f);

            return result;
        }

        #endregion
        #region Specials

        public override string ToString()
        {
            return  $"{Network_Name} ({NetworkCopyIndex})" ?? "Unsaved network";
        }

        public static NeuralNetwork LoadFromFile(string filePath)
        {

            NeuralNetwork neuralNetwork = null;
            if (File.Exists(filePath))
            {
                string jsonText = File.ReadAllText(filePath);
                SerializedNetwork serializedNetwork = JsonUtility.FromJson<SerializedNetwork>(jsonText);
                neuralNetwork = serializedNetwork.UnserializeNetwork();
            }
            return neuralNetwork ?? throw new Exception("Loading failed!");
        }
        
        public void Save(string name = null, bool owerride = false)
        {

            if (name is null)
            {
                if (Network_Name is null)
                {
                    throw new Exception("Network name is empty!");
                }
            }
            else
            {
                Network_Name = name;
            }

            string fileName = NeuralNetworks_Manager.GetNetworkFileName(this);
            if (!owerride)
            {
                while (File.Exists(fileName))
                {
                    NetworkCopyIndex++;
                    fileName = NeuralNetworks_Manager.GetNetworkFileName(this);
                }
            }
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                SerializedNetwork serializedNetwork = new SerializedNetwork(this);
                string jsonString = JsonUtility.ToJson(serializedNetwork, true);
                writer.Write(jsonString);
                writer.Close();
            }
        }
        public void Delete()
        {
            string fileName = NeuralNetworks_Manager.GetNetworkFileName(this);

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        #endregion
    }
}