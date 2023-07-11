using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Game {
	public class HumanPlayer : Player {

		public enum InputState {
			None,
			PieceSelected,
			DraggingPiece
		}

		InputState currentState;

		BoardUI boardUI;
		public Camera cam;
		Coord selectedPieceSquare;
		Board board;
		public HumanPlayer (Board board) {
			boardUI = GameObject.FindObjectOfType<BoardUI> ();
			cam = Camera.main;
			this.board = board;
		}

		public override void NotifyTurnToMove () {

		}

		public override void Update () {
			HandleInput ();
		}

		void HandleInput () {
			Vector2 mousePos;
			if (cam != null)
			{
				mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
			}
			else
            {
				mousePos = Vector2.zero;
            }

			Vector2 boardPos = mousePos - (Vector2)boardUI.transform.position; ;

			if (currentState == InputState.None) {
				HandlePieceSelection (boardPos);
			} else if (currentState == InputState.DraggingPiece) {
				HandleDragMovement (mousePos, boardPos);
			} else if (currentState == InputState.PieceSelected) {
				HandlePointAndClickMovement (boardPos);
			}

			if (Input.GetMouseButtonDown (1)) {
				CancelPieceSelection ();
			}
		}

		void HandlePointAndClickMovement (Vector2 mousePos) {
			if (Input.GetMouseButton (0)) {
				HandlePiecePlacement (mousePos);
			}
		}

		void HandleDragMovement (Vector2 mousePos, Vector2 boardPos) {
			boardUI.DragPiece (selectedPieceSquare, mousePos);
			// If mouse is released, then try place the piece
			if (Input.GetMouseButtonUp (0)) {
				HandlePiecePlacement (boardPos);
			}
		}

		void HandlePiecePlacement (Vector2 mousePos) {
			Coord targetSquare;
			if (boardUI.TryGetSquareUnderMouse (mousePos, out targetSquare)) {
				if (targetSquare.Equals (selectedPieceSquare)) {
					boardUI.ResetPiecePosition (selectedPieceSquare);
					if (currentState == InputState.DraggingPiece) {
						currentState = InputState.PieceSelected;
					} else {
						currentState = InputState.None;
						boardUI.DeselectSquare (selectedPieceSquare);
					}
				} else {
					int targetIndex = BoardRepresentation.IndexFromCoord (targetSquare.fileIndex, targetSquare.rankIndex);
					if (Piece.IsColour (board.Square[targetIndex], board.ColourToMove) && board.Square[targetIndex] != 0) {
						CancelPieceSelection ();
						HandlePieceSelection (mousePos);
					} else {
						TryMakeMove (selectedPieceSquare, targetSquare);
					}
				}
			} else {
				CancelPieceSelection ();
			}

		}

		void CancelPieceSelection () {
			if (currentState != InputState.None) {
				currentState = InputState.None;
				boardUI.DeselectSquare (selectedPieceSquare);
				boardUI.ResetPiecePosition (selectedPieceSquare);
			}
		}

		void TryMakeMove (Coord startSquare, Coord targetSquare) {
			int startIndex = BoardRepresentation.IndexFromCoord (startSquare);
			int targetIndex = BoardRepresentation.IndexFromCoord (targetSquare);
			bool moveIsLegal = false;
			Move chosenMove = new Move ();

			MoveGenerator moveGenerator = new MoveGenerator ();
			bool wantsKnightPromotion = Input.GetKey (KeyCode.LeftAlt);

			var legalMoves = moveGenerator.GenerateMoves (board);
			for (int i = 0; i < legalMoves.Count; i++) {
				var legalMove = legalMoves[i];

				if (legalMove.StartSquare == startIndex && legalMove.TargetSquare == targetIndex) {
					if (legalMove.IsPromotion) {
						if (legalMove.MoveFlag == Move.Flag.PromoteToQueen && wantsKnightPromotion) {
							continue;
						}
						if (legalMove.MoveFlag != Move.Flag.PromoteToQueen && !wantsKnightPromotion) {
							continue;
						}
					}
					moveIsLegal = true;
					chosenMove = legalMove;
					//	Debug.Log (legalMove.PromotionPieceType);
					break;
				}
			}

			if (moveIsLegal) {
				ChoseMove (chosenMove);
				currentState = InputState.None;
			} else {
				CancelPieceSelection ();
			}
		}

		void HandlePieceSelection (Vector2 mousePos) {
			if (Input.GetMouseButtonDown (0)) {
				if (boardUI.TryGetSquareUnderMouse (mousePos, out selectedPieceSquare)) {
					int index = BoardRepresentation.IndexFromCoord (selectedPieceSquare);
					// If square contains a piece, select that piece for dragging
					if (Piece.IsColour (board.Square[index], board.ColourToMove)) {
						boardUI.HighlightLegalMoves (board, selectedPieceSquare);
						boardUI.SelectSquare (selectedPieceSquare);
						currentState = InputState.DraggingPiece;
					}
				}
			}
		}
	}
}