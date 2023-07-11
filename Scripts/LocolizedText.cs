using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocolizedText : MonoBehaviour
{
    public string EngText;
    public string RusText;

    private TMP_Text Text;
    private LanguagesManager LanguagesManager;

    public void Awake()
    {
        LanguagesManager = GameObject.FindWithTag("LanguageManager").GetComponent<LanguagesManager>();
        Text = GetComponent<TMP_Text>();

        LanguagesManager.OnLanguageChainged += LanguagesManager_OnLanguageChainged;
    }
    public void Start()
    {
        UpdateText();
    }

    void UpdateText()
    {
        LanguagesManager_OnLanguageChainged(LanguagesManager.CurentLanguage);
        
    }

    private void LanguagesManager_OnLanguageChainged(LanguagesManager.Language language)
    {
        bool isActive = gameObject.activeSelf;
        gameObject.SetActive(true);

        switch (language)
        {
            case LanguagesManager.Language.Russian:
                Text.text = RusText;
                break;

            case LanguagesManager.Language.English:
            default:
                Text.text = EngText;
                break;
        }
        gameObject.SetActive(isActive);
        Debug.Log(1);
    }
}
