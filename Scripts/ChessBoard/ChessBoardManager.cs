using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Game
{
    public class ChessBoardManager : MonoBehaviour
    {
        public GameManager ChessBoard;
        public BoardUIManager UIManager;

        public static float Width = 13.25f;
        public static float Height = 12.25f;

        public string White;
        public string Black;

        public void Start()
        {
            ChessBoard.onGameFinished += ChessBoard_onGameFinished;
        }

        public virtual void ChessBoard_onGameFinished(GameManager.Result obj)
        {
            ChessBoard.whiteClock.secondsRemaining = 0;
            ChessBoard.blackClock.secondsRemaining = 0;
        }
    }
}