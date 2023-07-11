using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Game {
	public abstract class Player {
		public event System.Action<Move> OnMoveChosen;

		public abstract void Update ();

		public abstract void NotifyTurnToMove ();

		protected virtual void ChoseMove (Move move) {
			OnMoveChosen?.Invoke (move);
		}

        public override string ToString()
        {
			return "Player";
        }
    }
}