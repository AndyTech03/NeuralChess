using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Game {
    public class NeuralNetwork_ListItemManager : MonoBehaviour
    {
        public Button Delete_Button;
        public TMP_Text NameTextField;
        public TMP_Text DetailTextField;

        private LanguagesManager LanguagesManager;
        private NeuralNetwork neuralNetwork;

        public void Awake()
        {
            LanguagesManager = GameObject.FindWithTag("LanguageManager").GetComponent<LanguagesManager>();
        }

        internal void SetData(NeuralNetwork network)
        {
            NameTextField.text = network.ToString();
            neuralNetwork = network;
            DetailTextField.text = FormatDetail(network);
            Delete_Button.onClick.AddListener(RemoveNetworkFiles);
        }

    string FormatDetail(NeuralNetwork network)
        {
            string networkLayersData = "";
            string networkLayersActivations = "";
            for (int l = 0; l < network.LayersStructure.Length; l++)
            {
                networkLayersData = $"{networkLayersData} {network.LayersStructure[l]}";
                if (l != 0)
                    networkLayersActivations = $"{networkLayersActivations} {network.ActivationsStructure[l - 1]}";
            }
            string detail;
            switch (LanguagesManager.CurentLanguage)
            {
                case LanguagesManager.Language.Russian:
                    detail =
                        $"В/Н/П:\t\t    {network.Wins}/{network.Draws}/{network.Losses}\n" +
                        $"Размеры слоёв:\t   {networkLayersData}\n" +
                        $"Функции активации: {networkLayersActivations}";
                    break;
                case LanguagesManager.Language.English:
                default:
                    detail =
                        $"W/D/L:        {network.Wins}/{network.Draws}/{network.Losses}\n" +
                        $"Layers sizes: {networkLayersData}\n" +
                        $"Activations:  {networkLayersActivations}";
                    break;
            }
            return detail;
        }

        void RemoveNetworkFiles()
        {
            neuralNetwork.Delete();
        }
    }
}