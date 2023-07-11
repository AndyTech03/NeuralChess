using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess {

	public class EvaluationLog
    {
		public int Evaluation;
		public string BoardFen;
		public EvaluationLog(int eval, string fen)
        {
			Evaluation = eval;
			BoardFen = fen;
        }
    }
	public interface IEvaluation
	{
		event System.Action<EvaluationLog> OnEvaluation;

		int Evaluate(Board board);
	}

	public interface ILogWriter
    {
		void WriteLogData(EvaluationLog data, string path);
    }

	public interface ILogReader
    {
		List<EvaluationLog> ReadLogData(string path);
    }

	public class NetWorkEvaluation : IEvaluation
	{
		public delegate void Training();
		public Game.AIPlayer Trainer;
		public NeuralNetwork Brain;
		public int ID;

        public event Action<EvaluationLog> OnEvaluation;
		public event Action<int, string, int, int> OnEvaluateWithTrainer;

        string Name
		{
			get => Brain != null ? Brain.ToString() : "AI";
		}

        static float[] ValidateInputs(int[] data)
		{
			const float VoidValue = .01f;
			const float PawnValue = .1f;
			const float RookValue = .2f;
			const float KnightValue = .3f;
			const float BishopValue = .4f;
			const float QueenValue = .7f;
			const float KingValue = .9f;

			float[] result = new float[data.Length];
			//string log = "";
			for (int i = 0; i < data.Length; i++)
			{
                switch (data[i])
                {
					case 0:							// Void
						result[i] = VoidValue;
						break;

					case 9:							// White King
						result[i] = KingValue;
						break;
					case 10:						// White Pawn
						result[i] = PawnValue;
						break;
					case 11:						// White Knight
						result[i] = KnightValue;
						break;
					case 13:						// White Bishop
						result[i] = BishopValue;
						break;
					case 14:						// White Rook
						result[i] = RookValue; 
						break;
					case 15:						// White Queen
						result[i] = QueenValue;
						break;

					case 17:                        // Black King
						result[i] = -KingValue;
						break;
					case 18:						// Black Pawn
						result[i] = -PawnValue;
						break;
					case 19:                        // Black Knight
						result[i] = -KnightValue;
						break;
					case 21:                        // Black Bishop
						result[i] = -BishopValue;
						break;
					case 22:						// Black Rook
						result[i] = -RookValue;
						break;
					case 23:						// Black Queen
						result[i] = -QueenValue;
						break;

					default:
						result[i] = data[i];
						break;
                }

				//log += $"{result[i]} ";
			}
			//Debug.LogError(log);
			return result;
		}

		public static float[] TwoBoards(int[] inputs)
        {
			float[] data = ValidateInputs(inputs);
			float[] whiteBoard = new float[data.Length];
			float[] blackBoard = new float[data.Length];
			for (int i = 0; i<data.Length; i++)
            {
				float value = data[i];
				if (value > 0)
				{
					whiteBoard[i] = value;
					blackBoard[i] = 0.01f;
				}
				else if(value < 0)
				{
					whiteBoard[i] = 0.01f;
					blackBoard[i] = value;
                }
                else
                {
					whiteBoard[i] = 0.01f;
					blackBoard[i] = 0.01f;
				}
            }

			List<float> result = whiteBoard.ToList();
			result.AddRange(blackBoard);
			return result.ToArray();
		}

		public static float[] OneBoard(int[] inputs)
        {
			return ValidateInputs(inputs);
        }

		public override string ToString()
		{
			return Name;
		}

		public static int ValidateOutputs(float[] data)
		{
			return (int)(data[0] * 10000);
		}

		public static float[] ValidateTarget(int target)
		{
			if (target >= 9000)
				target = 9000;
			if (target <= -9000)
				target = -9000;
			return new float[] { target / 10000f };
		}

		public int Evaluate(Board board)
		{ 
			int perspective = (board.WhiteToMove) ? 1 : -1;

			float[] inputs;
			if (Brain.LayersStructure[0] == 64)
            {
				 inputs = OneBoard(board.Square);
			}
			else if(Brain.LayersStructure[0] == 128)
            {
				inputs = TwoBoards(board.Square);
            }
			else
            {
				inputs = ValidateInputs(board.Square);
            }


			float[] outputs = Brain.FeedForward(inputs);

			int result = ValidateOutputs(outputs) * perspective;

			string log = $"{this}\n	result: {result};\n";

			if (Trainer != null)
			{
				int target = Trainer.search.evaluation.Evaluate(board) * perspective;
				string fen = FenUtility.CurrentFen(board);

				OnEvaluateWithTrainer?.Invoke(ID ,fen, result, target);

				log += $"	target: {target};";
			}
			//Debug.Log(log);
			int min = 10;

			result = 
				(result > min ? result : 
				(result < -min ? result : 
				(result > 0 ? min : -min
			)));

			OnEvaluation?.Invoke(new EvaluationLog(result, FenUtility.CurrentFen(board)));

			return result;
		}
    }

    public class Evaluation : IEvaluation
	{

		public const int pawnValue = 100;
		public const int knightValue = 300;
		public const int bishopValue = 320;
		public const int rookValue = 500;
		public const int queenValue = 900;

		const float endgameMaterialStart = rookValue * 2 + bishopValue + knightValue;
		Board board;

        public event Action<EvaluationLog> OnEvaluation;

        public override string ToString()
        {
            return "Base AI";
        }

        // Performs static evaluation of the current position.
        // The position is assumed to be 'quiet', i.e no captures are available that could drastically affect the evaluation.
        // The score that's returned is given from the perspective of whoever's turn it is to move.
        // So a positive score means the player who's turn it is to move has an advantage, while a negative score indicates a disadvantage.
        public int Evaluate (Board board) {
			this.board = board;
			int whiteEval = 0;
			int blackEval = 0;

			//подсчёт стоимостей всех фигур
			int whiteMaterial = CountMaterial (Board.WhiteIndex);
			int blackMaterial = CountMaterial (Board.BlackIndex);

			//Без пешек
			int whiteMaterialWithoutPawns = whiteMaterial - board.pawns[Board.WhiteIndex].Count * pawnValue;
			int blackMaterialWithoutPawns = blackMaterial - board.pawns[Board.BlackIndex].Count * pawnValue;


			float whiteEndgamePhaseWeight = EndgamePhaseWeight (whiteMaterialWithoutPawns);
			float blackEndgamePhaseWeight = EndgamePhaseWeight (blackMaterialWithoutPawns);

			///Первичная оценка
			whiteEval += whiteMaterial;
			blackEval += blackMaterial;

			whiteEval += MopUpEval (Board.WhiteIndex, Board.BlackIndex, whiteMaterial, blackMaterial, blackEndgamePhaseWeight);
			blackEval += MopUpEval (Board.BlackIndex, Board.WhiteIndex, blackMaterial, whiteMaterial, whiteEndgamePhaseWeight);

			whiteEval += EvaluatePieceSquareTables (Board.WhiteIndex, blackEndgamePhaseWeight);
			blackEval += EvaluatePieceSquareTables (Board.BlackIndex, whiteEndgamePhaseWeight);

			int eval = whiteEval - blackEval;

			int perspective = (board.WhiteToMove) ? 1 : -1;

			OnEvaluation?.Invoke(new EvaluationLog(eval, FenUtility.CurrentFen(board)));

			return eval * perspective;
		}
		/// <summary>
		/// Если стоимость фигур без пешек меньше чем 2 ладьи слон и король возвращает число < 1
		/// </summary>
		/// <param name="materialCountWithoutPawns">Стоимость фигур без пешек</param>
		/// <returns>Вес фигуры в поздней игре</returns>
		float EndgamePhaseWeight (int materialCountWithoutPawns) {
			const float multiplier = 1 / endgameMaterialStart;
			return 1 - System.Math.Min (1, materialCountWithoutPawns * multiplier);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="friendlyIndex">Индекс союзных</param>
		/// <param name="opponentIndex">Индекс противников</param>
		/// <param name="myMaterial">Мои ресурсы</param>
		/// <param name="opponentMaterial">Ресурсы противника</param>
		/// <param name="endgameWeight">Множитель поздней игры</param>
		/// <returns>Если идёт поздняя игра возвращает число зависящее от удаления короля от центра, от короля противника, тем меньше чем меньше осталось ресурсов, иначе 0</returns>
		int MopUpEval (int friendlyIndex, int opponentIndex, int myMaterial, int opponentMaterial, float endgameWeight) {
			int mopUpScore = 0;

			/// Если у меня больше чем у врага + 2 пешки и множитель поздней игры > 0
			if (myMaterial > opponentMaterial + pawnValue * 2 && endgameWeight > 0) {

				int friendlyKingSquare = board.KingSquare[friendlyIndex];
				int opponentKingSquare = board.KingSquare[opponentIndex];
				mopUpScore += PrecomputedMoveData.centreManhattanDistance[opponentKingSquare] * 10; ///Расстояние манхетенна от центра до позиции короля * 10
				// use ortho dst to promote direct opposition
				mopUpScore += (14 - PrecomputedMoveData.NumRookMovesToReachSquare (friendlyKingSquare, opponentKingSquare)) * 4; /// (14 - растояние между королями) * 4

				return (int) (mopUpScore * endgameWeight);
			}
			return 0;
		}

		int CountMaterial (int colourIndex) {
			int material = 0;
			material += board.pawns[colourIndex].Count * pawnValue;
			material += board.knights[colourIndex].Count * knightValue;
			material += board.bishops[colourIndex].Count * bishopValue;
			material += board.rooks[colourIndex].Count * rookValue;
			material += board.queens[colourIndex].Count * queenValue;

			return material;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="colourIndex">Индекс союзного цвета</param>
		/// <param name="endgamePhaseWeight">Множитель поздней игры</param>
		/// <returns>Оценка позиция фигур</returns>
		int EvaluatePieceSquareTables (int colourIndex, float endgamePhaseWeight) {
			int value = 0;
			bool isWhite = colourIndex == Board.WhiteIndex;

			/// Оценка позиций всех фигур
			value += EvaluatePieceSquareTable (PieceSquareTable.pawns, board.pawns[colourIndex], isWhite);
			value += EvaluatePieceSquareTable (PieceSquareTable.rooks, board.rooks[colourIndex], isWhite);
			value += EvaluatePieceSquareTable (PieceSquareTable.knights, board.knights[colourIndex], isWhite);
			value += EvaluatePieceSquareTable (PieceSquareTable.bishops, board.bishops[colourIndex], isWhite);
			value += EvaluatePieceSquareTable (PieceSquareTable.queens, board.queens[colourIndex], isWhite);

			/// Оценка позици короля в ранней игре
			int kingEarlyPhase = PieceSquareTable.Read (PieceSquareTable.kingMiddle, board.KingSquare[colourIndex], isWhite);

			/// чем меньше ресурсов тем менее значима позиция поряля
			value += (int) (kingEarlyPhase * (1 - endgamePhaseWeight));
			//value += PieceSquareTable.Read (PieceSquareTable.kingMiddle, board.KingSquare[colourIndex], isWhite);

			return value;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="table">Таблица оценок</param>
		/// <param name="pieceList">Список моих фигур</param>
		/// <param name="isWhite">Мой цвет белый?</param>
		/// <returns>Оценка позиций фигур одного типа</returns>
		static int EvaluatePieceSquareTable (int[] table, PieceList pieceList, bool isWhite) {
			int value = 0;
			for (int i = 0; i < pieceList.Count; i++) {
				value += PieceSquareTable.Read (table, pieceList[i], isWhite);
			}
			return value;
		}
	}
}