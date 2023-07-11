using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Game {
    public class ChessBoardsFactory : MonoBehaviour
    {
        private List<ChessBoardManager> BoardManagers;

        public void Awake()
        {
            BoardManagers = new List<ChessBoardManager>();
        }
        public ChessBoardManager CreateBoard(GameObject template, Transform parent, Vector3 pos)
        {
            ChessBoardManager temp = Instantiate(template, pos, Quaternion.identity, parent).GetComponent<ChessBoardManager>();
            BoardManagers.Add(temp);
            return temp;
        }

        public void RemoveBoard(ChessBoardManager boardManager)
        {
            BoardManagers.Remove(boardManager);
        }

        public void Clear()
        {
            BoardManagers.Clear();
        }
    }
}