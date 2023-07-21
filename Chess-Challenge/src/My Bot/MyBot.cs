using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    public class MyBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        public Move Think(Board board, Timer timer)
        {
            int depth = 4;
            int alpha = int.MinValue;  // Initial value for alpha (negative infinity)
            int beta = int.MaxValue;   // Initial value for beta (positive infinity)
            Move moveToPlay = RecursiveSearchGPT(board, depth, alpha, beta);
            return moveToPlay;
        }


        // Recursive search function
        Move RecursiveSearch(Board board, Move bestMove, int depth)
        {
            if (depth == 0)
            {
                Console.WriteLine($"Depth = 0, returning {bestMove}");
                return bestMove;
            }

            int currentEval = Evaluate(board);

            Move[] allMoves = board.GetLegalMoves();
            foreach (Move move in allMoves)
            {
                // Always play checkmate in one
                if (MoveIsCheckmate(board, move))
                {
                    return move;
                }

                // Do move
                board.MakeMove(move);

                // Evaluate new move
                if (Evaluate(board) > currentEval)
                {
                    bestMove = move;
                }

                // Undo move
                board.UndoMove(move);
            }

            // Recurse
            bestMove = RecursiveSearch(board, bestMove, depth - 1);

            return bestMove;
        }

        // Alpha-beta pruning implementation of RecursiveSearch
        Move RecursiveSearchGPT(Board board, int depth, int alpha, int beta)
        {
            if (depth == 0)
            {
                Console.WriteLine($"Depth = 0, returning");
                return new Move(); // Return null instead of bestMove in this case
            }

            Move[] allMoves = board.GetLegalMoves();

            // For simplicity, assume it's the opponent's turn at even depths
            bool isMaximizing = depth % 2 == 1;

            if (isMaximizing)
            {
                int bestEval = int.MinValue;
                Move bestMove = new Move();

                foreach (Move move in allMoves)
                {
                    // Always play checkmate in one
                    if (MoveIsCheckmate(board, move))
                    {
                        return move;
                    }

                    board.MakeMove(move);

                    // Evaluate the move based on the board state after the move
                    int eval = Evaluate(board);

                    board.UndoMove(move);

                    if (eval > bestEval)
                    {
                        bestEval = eval;
                        bestMove = move;
                    }

                    alpha = Math.Max(alpha, bestEval);
                    if (beta <= alpha)
                    {
                        break; // Beta cutoff
                    }
                }

                return bestMove;
            }
            else // Minimizing player's turn
            {
                int bestEval = int.MaxValue;
                Move bestMove = new Move();

                foreach (Move move in allMoves)
                {
                    // Always play checkmate in one
                    if (MoveIsCheckmate(board, move))
                    {
                        return move;
                    }

                    board.MakeMove(move);

                    // Evaluate the move based on the board state after the move
                    int eval = Evaluate(board);

                    board.UndoMove(move);

                    if (eval < bestEval)
                    {
                        bestEval = eval;
                        bestMove = move;
                    }

                    beta = Math.Min(beta, bestEval);
                    if (beta <= alpha)
                    {
                        break; // Alpha cutoff
                    }
                }

                return bestMove;
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

        // Simple evaluation function of the board.
        // It sums the values of the pieces.
        int Evaluate(Board board)
        {
            int score = 0;
            int highestValueCapture = 0;

            for (int i = 0; i <= 63; i++)
            {
                Piece piece = board.GetPiece(new Square(i));
                int pieceVal = pieceValues[(int)piece.PieceType];
                if (pieceVal > 0)
                {
                    score += pieceVal;
                }
            }

            foreach (Move move in board.GetLegalMoves())
            {
                // Find highest value capture
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

                if (capturedPieceValue > highestValueCapture)
                {
                    highestValueCapture = capturedPieceValue;
                }
            }

            return score + 2 * highestValueCapture;
        }
    }
}