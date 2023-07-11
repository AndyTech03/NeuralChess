using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PlayerPrefsManger;

namespace Chess.Game
{
    public class MainManu_UIManager : MonoBehaviour
    {

        public Camera MainCamera;

        public GameObject MainMenu_Panel;
        public Button Start_NetworkTraining_Button;
        public Button Create_NeuralNetworksLobby_Button;
        public Button SettingsButton;
        public Button Quit_Button;

        public GameObject NeuralNetworksLobby_Panel;
        public GameObject NeuralNetwork_ListItem_Prefab;
        public GameObject NeuralNetworks_Container;
        List<GameObject> listItems;
        public Button Leave_NeuralNetworksLobby_Button;
        public TMP_InputField NewNetworkName_Input;
        public Toggle OneBoard_Toggle;
        public Button Save_NeuralNetwork_Button;
        public Button CancelNewNetwork_Button;
        public Button NewLayer_Button;
        public Button AcceptLayer_Button;
        public Button CancelLayer_Button;
        public TMP_Text Layers_Text;

        public GameObject AddLayerPanel;
        public TMP_Dropdown Activation_DropDown;
        public TMP_InputField LayerSize_Input;

        public GameObject TrainBoard_Prefab;
        public GameObject DuelBoard_Prefab;

        private GameObject[] panels;

        public GameObject SettingsPanell;
        public TMP_InputField GenerationMatchDuration;
        public TMP_InputField DuelMatchDuration;
        public TMP_InputField DuelIterations;
        public TMP_InputField RepiatsCount;
        public TMP_InputField DatasetSize;
        public TMP_Dropdown Languages_Dropdown;
        public Button BackButton;

        public LanguagesManager LanguagesManager;


        #region Init

        private void Awake()
        {
            panels = new GameObject[] { MainMenu_Panel, NeuralNetworksLobby_Panel, SettingsPanell };

            Start_NetworkTraining_Button.onClick.AddListener(Start_TrainScene_Loading);
            Create_NeuralNetworksLobby_Button.onClick.AddListener(Create_NeuralNetworks_Lobby);
            Quit_Button.onClick.AddListener(QuitGame);

            Save_NeuralNetwork_Button.onClick.AddListener(Create_NeuralNetwork);
            CancelNewNetwork_Button.onClick.AddListener(CancelAddNetwork);
            NewLayer_Button.onClick.AddListener(Show_AddLayer_Dialog);
            AcceptLayer_Button.onClick.AddListener(AcceptNewLayer);
            CancelLayer_Button.onClick.AddListener(CancelNewLayer);

            AddLayerPanel.SetActive(false);
            Activation_DropDown.ClearOptions();
            Activation_DropDown.AddOptions(NeuralNetworks_Manager.ActivationsNames.ToList());

            Leave_NeuralNetworksLobby_Button.onClick.AddListener(LeaveLobby);
            listItems = new List<GameObject>();

            SettingsButton.onClick.AddListener(ShowSettings);
            Languages_Dropdown.value = (int)LanguagesManager.CurentLanguage;
            BackButton.onClick.AddListener(HideSettings);
        }

        public void Start()
        {
            ShowPanel(MainMenu_Panel);
        }


        private void ShowPanel(GameObject panel)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != panel)
                {
                    panels[i].SetActive(false);
                }
                else
                {
                    panels[i].SetActive(true);
                }

            }
        }

        #endregion

        #region Settings

        void ShowSettings()
        {
            ShowPanel(SettingsPanell);

            RepiatsCount.text = PlayerPrefs.GetInt(RepiatsCount_KeyName, 3).ToString();
            DuelIterations.text = PlayerPrefs.GetInt(DuelIteration_KeyName, 3).ToString();
            GenerationMatchDuration.text = PlayerPrefs.GetInt(GenerationMatchDuration_KeyName, 60).ToString();
            DatasetSize.text = PlayerPrefs.GetInt(DatasetSize_KeyName, 1000).ToString();
            DuelMatchDuration.text= PlayerPrefs.GetInt(DuelMatchDuration_KeyName, 150).ToString();
        }

        void HideSettings()
        {
            AplySettings();
            ShowPanel(MainMenu_Panel);
        }

        void AplySettings()
        {
            int repiatsCount = int.Parse(RepiatsCount.text);
            int datsetSize = int.Parse(DatasetSize.text);
            int duelIterations = int.Parse(DuelIterations.text);
            int generationsMatcesduration = int.Parse(GenerationMatchDuration.text);
            int duelMatchDuration = int.Parse(DuelMatchDuration.text);

            PlayerPrefs.SetInt(RepiatsCount_KeyName, repiatsCount);
            PlayerPrefs.SetInt(DatasetSize_KeyName, datsetSize);
            PlayerPrefs.SetInt(DuelIteration_KeyName, duelIterations);
            PlayerPrefs.SetInt(GenerationMatchDuration_KeyName, generationsMatcesduration);
            PlayerPrefs.SetInt(DuelMatchDuration_KeyName, duelMatchDuration);

            LanguagesManager.CurentLanguage = (LanguagesManager.Language)Languages_Dropdown.value;
        }

        #endregion

        #region Start Network Training

        void Start_TrainScene_Loading()
        {
            SceneManager.LoadSceneAsync(1);
        }

        #endregion

        #region NeuralNetworksLobby

        void Create_NeuralNetworks_Lobby()
        {
            ShowPanel(NeuralNetworksLobby_Panel);
            RefreshNeuralNetworksViewList();
        }

        void SaveDataFromDialog()
        {
            if (int.TryParse(LayerSize_Input.text, out int size))
            {
                string activationName = NeuralNetworks_Manager.ActivationsNames[Activation_DropDown.value];
                Layers_Text.text += $"{activationName}:{size}\n";
            }
        }

        void HideNewLayerDialog()
        {
            AddLayerPanel.SetActive(false);
        }

        void ClearDialog()
        {
            LayerSize_Input.text = "";
            Activation_DropDown.value = 0;
        }

        void AcceptNewLayer()
        {
            SaveDataFromDialog();
            ClearDialog();
            HideNewLayerDialog();
        }

        void CancelNewLayer()
        {
            ClearDialog();
            HideNewLayerDialog();
        }

        void Show_AddLayer_Dialog()
        {
            if (AddLayerPanel.activeSelf)
            {
                SaveDataFromDialog();
                ClearDialog();
            }
            else
            {
                AddLayerPanel.SetActive(true);
            }
        }

        void CancelAddNetwork()
        {
            CancelNewLayer();
            ClearNewNetworkData();
        }

        void ClearNewNetworkData()
        {
            NewNetworkName_Input.text = "";
            OneBoard_Toggle.isOn = true;
            Layers_Text.text = "";
        }

        void Create_NeuralNetwork()
        {
            string netName = NewNetworkName_Input.text;
            if (netName != "")
            {
                string[] rowData = Layers_Text.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                int[] layers = new int[rowData.Length+2];
                string[] activations = new string[layers.Length - 1];


                for (int i = 0; i<layers.Length; i++)
                {
                    if (i == 0)
                    {
                        layers[i] = OneBoard_Toggle.isOn ? NeuralNetworks_Manager.InputSizes[0] : NeuralNetworks_Manager.InputSizes[1];
                    }
                    else if (i == layers.Length - 1)
                    {
                        activations[i - 1] = NeuralNetworks_Manager.ActivationsNames[0];
                        layers[i] = NeuralNetworks_Manager.OutputSizes[0];
                    }
                    else
                    {
                        string[] data = rowData[i - 1].Split(':');
                        activations[i - 1] = data[0];
                        layers[i] = int.Parse(data[1]);
                    }
                }

                new NeuralNetwork(layers, activations, netName).Save();
                ClearNewNetworkData();

                RefreshNeuralNetworksViewList();
            }
        }

        void RefreshNeuralNetworksViewList()
        {
            GameObject listItem;
            RectTransform rectTransform;
            NeuralNetwork_ListItemManager neuralNetwork_ListItem;
            List<NeuralNetwork> neuralNetworks = NeuralNetworks_Manager.GetAllNetworks_FromNetsDirectory();
            float itemsHeight = 0;

            if (listItems.Count !=0)
            {
                for (int i=0; i<listItems.Count; i++)
                {
                    Destroy(listItems[i]);
                }
                listItems.Clear();
            }

            for (int i=0; i< neuralNetworks.Count; i++)
            {
                listItem = Instantiate(NeuralNetwork_ListItem_Prefab, NeuralNetworks_Container.transform);
                listItems.Add(listItem);
                rectTransform = (RectTransform)listItem.transform;
                itemsHeight = (rectTransform.rect.height + 5) * i;
                rectTransform.localPosition -= new Vector3(0, itemsHeight);
                itemsHeight += rectTransform.rect.height;
                neuralNetwork_ListItem = listItem.GetComponent<NeuralNetwork_ListItemManager>();

                neuralNetwork_ListItem.SetData(neuralNetworks[i]);
                neuralNetwork_ListItem.Delete_Button.onClick.AddListener(RefreshNeuralNetworksViewList);
            }

            RectTransform rect = NeuralNetworks_Container.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, itemsHeight);
        }

        #endregion

        void LeaveLobby()
        {
            ShowPanel(MainMenu_Panel);
        }

        void QuitGame()
        {
            Application.Quit();
        }

        void Update()
        {

        }
    }
}