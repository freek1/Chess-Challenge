using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    public class MyBot : IChessBot
    {
        bool botIsWhite = true;
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        Random rng = new();

        public Move Think(Board board, Timer timer)
        {
            // TODO: Make everything take up less 'bot memory'
            int depth = 4;
            botIsWhite = board.IsWhiteToMove;

            Move moveToPlay = RecursiveSearch(board, depth);
            return moveToPlay;
        }

        Move RecursiveSearch(Board board, int depth)
        {
            Move bestMove = new Move();
            int bestEval = int.MinValue;

            Move[] legalMoves = board.GetLegalMoves();

            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int currentEval = -MiniMax(board, depth - 1, int.MinValue + 1, int.MaxValue);
                board.UndoMove(move);

                if (currentEval > bestEval)
                {
                    bestEval = currentEval;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        // TODO: find more efficient search method (alpha-beta/more advanced
        int MiniMax(Board board, int depth, int alpha, int beta)
        {
            if (depth == 0)
            {
                return Evaluate(board);
            }

            Move[] legalMoves = board.GetLegalMoves();

            if (botIsWhite)
            {
                int maxEval = int.MinValue;
                foreach (Move move in legalMoves)
                {
                    if (MoveIsCheckmate(board, move))
                    {
                        return int.MaxValue;
                    }
                    board.MakeMove(move);
                    int eval = MiniMax(board, depth - 1, alpha, beta);
                    board.UndoMove(move);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (Move move in legalMoves)
                {
                    if (MoveIsCheckmate(board, move))
                    {
                        return int.MinValue;
                    }
                    board.MakeMove(move);
                    int eval = MiniMax(board, depth - 1, alpha, beta);
                    board.UndoMove(move);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
                return minEval;
            }
        }

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        // TODO: implement the trained mini-nn weights as Eval function.
        // Simple evaluation function; sums the values of the bots pieces and subtracts the value of the oppoenent's pieces.
        int Evaluate(Board board)
        {
            int score = 0;

            PieceType[] pieceTypesList = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
            foreach (PieceType pieceType in pieceTypesList)
            {
                PieceList pieceListBot = board.GetPieceList(pieceType, botIsWhite);
                PieceList pieceListOpponent = board.GetPieceList(pieceType, !botIsWhite);
                score += pieceValues[(int)pieceType] * pieceListBot.Count;
                score -= pieceValues[(int)pieceType] * pieceListOpponent.Count;
            }

            return score;
        }
    }
}