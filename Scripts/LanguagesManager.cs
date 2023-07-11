using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerPrefsManger;

public class LanguagesManager : MonoBehaviour
{
    public enum Language { Russian, English };

    public event System.Action<Language> OnLanguageChainged;
    
    public Language CurentLanguage 
    {
        get
        {
            SystemLanguage systemLanguage = Application.systemLanguage;
            int sysLangID;
            switch (systemLanguage)
            {
                case (SystemLanguage.Russian):
                    sysLangID = 0;
                    break;

                case (SystemLanguage.English):
                default:
                    sysLangID = 1;
                    break;

            }

            int languageID = PlayerPrefs.GetInt(Language_KeyName, sysLangID);
            return (Language)languageID;
        }

        set
        {
            int languageID = (int)value;
            PlayerPrefs.SetInt(Language_KeyName, languageID);
            OnLanguageChainged?.Invoke(value);
        } 
    }

    public void Start()
    {
        OnLanguageChainged?.Invoke(CurentLanguage);
    }
}
