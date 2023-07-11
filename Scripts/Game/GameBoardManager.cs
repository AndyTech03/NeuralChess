using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Game
{
    public class GameBoardManager : MonoBehaviour
    {
         public GameManager BoardManager;

        void Start()
        {
            BoardManager.NewGame();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
