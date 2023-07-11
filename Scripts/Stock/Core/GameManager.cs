using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess.Game {
	public class GameManager : MonoBehaviour {

		public enum Result {Error, Stopped, Pause, Playing, WhiteIsMated, BlackIsMated, Stalemate, Repetition, FiftyMoveRule, InsufficientMaterial, TimeIsUp }

		public event System.Action onPositionLoaded;
		public event System.Action<Move> onMoveMade;
		public event System.Action<Result> onGameFinished;

		public bool UseAnimations;

		public enum PlayerType { Human, AI, NeuralNetAI }

		public bool loadCustomPosition;
		public string customPosition = "1rbq1r1k/2pp2pp/p1n3p1/2b1p3/R3P3/1BP2N2/1P3PPP/1NBQ1RK1 w - - 0 1";

		public PlayerType whitePlayerType;
		public PlayerType blackPlayerType;
		public AISettings aiSettings;
		public Color[] colors;

		public bool useClocks;
		public Clock whiteClock;
		public Clock blackClock;
		public TMPro.TMP_Text whiteAiDiagnosticsUI;
		public TMPro.TMP_Text blackAiDiagnosticsUI;
		public TMPro.TMP_Text resultUI;

		public Result gameResult;

		public Player whitePlayer;
		public Player blackPlayer;
		Player playerToMove;
		List<Move> gameMoves;
		public BoardUI Board_UI;
		public int TimerDuration;

		public ulong zobristDebug;
		public Board board { get; private set; }
		Board searchBoard; // Duplicate version of board used for ai search

        private void Awake()
        {
			whiteClock.isTurnToMove = false;
			blackClock.isTurnToMove = false;

			gameMoves = new List<Move>();
			board = new Board();
			searchBoard = new Board();

			CreatePlayer(ref whitePlayer, whitePlayerType);
			CreatePlayer(ref blackPlayer, blackPlayerType);

			gameResult = Result.Pause;
		}

		private void Start()
		{
		}

		void Update()
		{
			zobristDebug = board.ZobristKey;

			if (gameResult == Result.Playing)
			{
				if (whiteClock.secondsRemaining <= 0 || blackClock.secondsRemaining <= 0)
				{
					gameResult = Result.TimeIsUp;
					NotifyPlayerToMove();
					return;
				}

				playerToMove.Update();

				if (useClocks)
				{
					whiteClock.isTurnToMove = board.WhiteToMove;
					blackClock.isTurnToMove = !board.WhiteToMove;
				}
			}
		}

		public List<string> lastPos;

		void OnMoveChosen (Move move) {
			bool animateMove = UseAnimations && playerToMove is AIPlayer;
			if (playerToMove.GetType() == typeof(AIPlayer))
			{
				LogAIDiagnostics();
				((AIPlayer)playerToMove).search.EndSearch();
			}

			bool success = true;

            try
			{
				board.MakeMove(move);

			}
            catch
            {
				success = false;
            }

			if (success)
            {
				searchBoard.MakeMove(move);
				gameMoves.Add(move);
				onMoveMade?.Invoke(move);
				Board_UI.OnMoveMade(board, move, animateMove);
				lastPos.Add(FenUtility.CurrentFen(board));
				NotifyPlayerToMove();
			}
			else
			{
				int index = lastPos.Count < 10 ? 0 : lastPos.Count - 10;
				string fen = lastPos[index];
				CompareBoards(fen);
				lastPos.RemoveRange(index, lastPos.Count - index);
				playerToMove.NotifyTurnToMove();
			}

		}

		void CompareBoards(string fen)
        {
			board.LoadPosition(fen);
			searchBoard.LoadPosition(fen);
			Board_UI.UpdatePosition(board);
			Board_UI.ResetSquareColours();
			if (whitePlayer is AIPlayer white)
			{
				white.board.LoadPosition(fen);
				white.search.board.LoadPosition(fen);
			}
			if (blackPlayer is AIPlayer black)
			{
				black.board.LoadPosition(fen);
				black.search.board.LoadPosition(fen);
			}
		}

		public void StopGame(Result result)
		{
			//board.WhiteToMove = true;
			//board.ColourToMove = (board.WhiteToMove) ? Piece.White : Piece.Black;
			//board.OpponentColour = (board.WhiteToMove) ? Piece.Black : Piece.White;
			//board.ColourToMoveIndex = (board.WhiteToMove) ? 0 : 1;

			gameResult = Result.Pause;

			if (whitePlayer is AIPlayer player1)
            {
                player1.search.EndSearch();
            }

            if (blackPlayer is AIPlayer player)
            {
                player.search.EndSearch();
            }

            if (loadCustomPosition)
			{
				board.LoadPosition(customPosition);
				searchBoard.LoadPosition(customPosition);
			}
			else
			{
				board.LoadStartPosition();
				searchBoard.LoadStartPosition();
			}

			onPositionLoaded?.Invoke();
			Board_UI.UpdatePosition(board);
			Board_UI.ResetSquareColours();

			gameResult = result;
			PrintGameResult(gameResult);
		}

		public void NewGame ()
		{
			StopGame(Result.Playing);

            lastPos = new List<string>
            {
                FenUtility.CurrentFen(board)
            };

			whiteClock.secondsRemaining = whiteClock.startSeconds = TimerDuration;
			blackClock.secondsRemaining = blackClock.startSeconds = TimerDuration;

			whiteClock.isTurnToMove = true;
			blackClock.isTurnToMove = true;

			CompareBoards(FenUtility.CurrentFen(board));

			NotifyPlayerToMove ();

		}

		void LogAIDiagnostics () {
			string text = "";
			var d = board.WhiteToMove ? ((AIPlayer)whitePlayer).search.searchDiagnostics : ((AIPlayer)blackPlayer).search.searchDiagnostics;
			text += "AI Diagnostics\n";
			text += $"<color=#{ColorUtility.ToHtmlStringRGB(colors[1])}>{playerToMove}\n";
			text += $"<color=#{ColorUtility.ToHtmlStringRGB(colors[0])}>Depth Searched: {d.lastCompletedDepth}";
			text += $"\nPositions evaluated: {d.numPositionsEvaluated}";

			string evalString = "";
			if (d.isBook) {
				evalString = "Book";
			} else {
				float displayEval = d.eval / 100f;
				evalString = ($"{displayEval:00.00}").Replace (",", ".");
				if (Search.IsMateScore (d.eval)) {
					evalString = $"mate in {Search.NumPlyToMateFromScore(d.eval)} ply";
				}
			}



			text += $"\n<color=#{ColorUtility.ToHtmlStringRGB(colors[1])}>Eval: {evalString}";
			text += $"\n<color=#{ColorUtility.ToHtmlStringRGB(colors[2])}>Move: {d.moveVal}";

			if (board.WhiteToMove)
				whiteAiDiagnosticsUI.text = text;
			else
				blackAiDiagnosticsUI.text = text;
		}

		public void ExportGame () {
			string pgn = PGNCreator.CreatePGN (gameMoves.ToArray ());
			string baseUrl = "https://www.lichess.org/paste?pgn=";
			string escapedPGN = UnityEngine.Networking.UnityWebRequest.EscapeURL (pgn);
			string url = baseUrl + escapedPGN;

			Application.OpenURL (url);
			TextEditor t = new TextEditor ();
			t.text = pgn;
			t.SelectAll ();
			t.Copy ();
		}

		public void QuitGame () {
			Application.Quit ();
		}

		void NotifyPlayerToMove () {
			gameResult = GetGameState ();
			PrintGameResult (gameResult);

			if (gameResult == Result.Playing) {
				playerToMove = (board.WhiteToMove) ? whitePlayer : blackPlayer;
				playerToMove.NotifyTurnToMove ();

			} else {
				onGameFinished(gameResult);
			}
		}

		void PrintGameResult (Result result) {
			float subtitleSize = resultUI.fontSize * 0.75f;
			string subtitleSettings = $"<color=#787878> <size={subtitleSize}>";

			if (result == Result.Playing)
			{
				resultUI.text = "";
			}
			else
			{
				resultUI.text = $"{whitePlayer} {blackPlayer}\n";
				if (result == Result.WhiteIsMated || result == Result.BlackIsMated)
				{
					resultUI.text += $"Checkmate! {result}";
				}
				else if (result == Result.FiftyMoveRule)
				{
					resultUI.text += "Draw";
					resultUI.text += subtitleSettings + "\n(50 move rule)";
				}
				else if (result == Result.Repetition)
				{
					resultUI.text += "Draw";
					resultUI.text += subtitleSettings + "\n(3-fold repetition)";
				}
				else if (result == Result.Stalemate)
				{
					resultUI.text += "Draw";
					resultUI.text += subtitleSettings + "\n(Stalemate)";
				}
				else if (result == Result.InsufficientMaterial)
				{
					resultUI.text += "Draw";
					resultUI.text += subtitleSettings + "\n(Insufficient material)";
				}
				else if (result == Result.Error)
				{
					resultUI.text += "Draw";
					resultUI.text += subtitleSettings + "\n(Error)";
				}
				else if (result == Result.TimeIsUp)
				{
					resultUI.text += "Draw";
					resultUI.text += subtitleSettings + "\n(Time is over)";
				}
				else if (result == Result.Stopped)
                {
					resultUI.text += "Game stopped!";
                }
			}
		}

		Result GetGameState () {
			if (gameResult == Result.Error)
				return Result.Error;
			if (gameResult == Result.TimeIsUp)
				return Result.TimeIsUp;
			MoveGenerator moveGenerator = new MoveGenerator ();
			var moves = moveGenerator.GenerateMoves (board);

			if (useClocks)
					if (whiteClock.secondsRemaining <= 0 || blackClock.secondsRemaining <=0)
                    {
						return Result.TimeIsUp;
                    }

			// Look for mate/stalemate
			if (moves.Count == 0) {
				if (moveGenerator.InCheck ()) {
					return (board.WhiteToMove) ? Result.WhiteIsMated : Result.BlackIsMated;
				}
				return Result.Stalemate;
			}

			// Fifty move rule
			if (board.fiftyMoveCounter >= 100) {
				return Result.FiftyMoveRule;
			}

			// Threefold repetition
			int repCount = board.RepetitionPositionHistory.Count ((x => x == board.ZobristKey));
			if (repCount == 3) {
				return Result.Repetition;
			}

			// Look for insufficient material (not all cases implemented yet)
			int numPawns = board.pawns[Board.WhiteIndex].Count + board.pawns[Board.BlackIndex].Count;
			int numRooks = board.rooks[Board.WhiteIndex].Count + board.rooks[Board.BlackIndex].Count;
			int numQueens = board.queens[Board.WhiteIndex].Count + board.queens[Board.BlackIndex].Count;
			int numKnights = board.knights[Board.WhiteIndex].Count + board.knights[Board.BlackIndex].Count;
			int numBishops = board.bishops[Board.WhiteIndex].Count + board.bishops[Board.BlackIndex].Count;

			if (numPawns + numRooks + numQueens == 0) {
				if (numKnights == 1 || numBishops == 1) {
					return Result.InsufficientMaterial;
				}
			}

			return Result.Playing;
		}

		public void CreatePlayer (ref Player player, PlayerType playerType) {
			if (player != null) {
				player.OnMoveChosen -= OnMoveChosen;
			}

			if (playerType == PlayerType.Human) {
				player = new HumanPlayer (board);
			} else
			{
				if (playerType == PlayerType.NeuralNetAI)
					player = new AIPlayer(searchBoard, aiSettings, new NetWorkEvaluation());
				else
					player = new AIPlayer(searchBoard, aiSettings, new Evaluation());
			}
			
			player.OnMoveChosen += OnMoveChosen;
		}
	}
}