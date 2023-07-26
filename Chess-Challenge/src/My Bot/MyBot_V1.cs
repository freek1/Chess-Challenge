using ChessChallenge.API;
using System;
using System.Threading.Tasks; // not allowed :(
using System.Linq;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    public class MyBot_V1 : IChessBot
    {
        bool botIsWhite = true;
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        // Load model weights
        string[] stringWeights = System.IO.File.ReadAllLines("C:\\Users\\freek\\source\\repos\\Chess-Challenge\\Chess-Challenge\\src\\My Bot\\geohotz_20k_cpu.txt");
        //double[] weights;

        public Move Think(Board board, Timer timer)
        {
            // TODO: Make everything take up less 'bot memory'
            int depth = 1;

            botIsWhite = board.IsWhiteToMove;
            //weights = stringWeights.Select(double.Parse).ToArray();

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

            /*
            float[,,] bitboard = MakeBitboard(board); // Shape is 29 x 8 x 8

            float[,] filter1 = new float[3, 3]; // the first 3x3 weights, 32 times
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    filter1[i, j] = weights[i + j];
                }
            }

            // Inference from the network
            float[,,] x = new Convolution2DLayer(filter1, 29, 32, 1, 0).Forward(bitboard);
            x = ApplyReLU(x);

            */

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
        /*
        public static float[,,] ApplyReLU(float[,,] tensor)
        {
            int dim1 = tensor.GetLength(0);
            int dim2 = tensor.GetLength(1);
            int dim3 = tensor.GetLength(2);

            float[] flattenedTensor = tensor.Cast<float>().ToArray();
            float[] reluValues = flattenedTensor.Select(val => Math.Max(0, val)).ToArray();
            float[,,] resultTensor = new float[dim1, dim2, dim3];

            for (int i = 0; i < reluValues.Length; i++)
            {
                resultTensor[i / dim2 / dim3, (i / dim3) % dim2, i % dim3] = reluValues[i];
            }

            return resultTensor;
        }


    // Converts board to [29 x 8 x 8] bitboard notation used when the network was trained (in python).
    float[,,] MakeBitboard(Board board)
        {
            float[,,] bitboard = new float[29, 8, 8];

            // Set the first dimensions of bitboard
            FillFirstDim(bitboard, 0, botIsWhite ? 1 : 0); 
            FillFirstDim(bitboard, 25, board.HasKingsideCastleRight(true) ? 1 : 0); // White King Castle
            FillFirstDim(bitboard, 26, board.HasQueensideCastleRight(true) ? 1 : 0); // White Queen Castle
            FillFirstDim(bitboard, 27, board.HasKingsideCastleRight(false) ? 1 : 0); // Black King Castle
            FillFirstDim(bitboard, 28, board.HasQueensideCastleRight(false) ? 1 : 0); // White Queen Castle

            PieceType[] pieceTypesList = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };

            // Track the corresponding dimension for filling the bitboard array.
            int dimension = 1;
            // First loop over white pieces, then black pieces.
            // This results in {P, N, B, R, Q, p, n, b, r, q}
            for (int whiteblack = 0; whiteblack < 2; whiteblack++)
            {
                foreach (PieceType pieceType in pieceTypesList)
                {
                    // Store pieces in the appropriate dimension
                    ulong pieceBitboard = board.GetPieceBitboard(pieceType, Convert.ToBoolean(whiteblack));
                    for (int i = 0; i < 64; i++)
                    {
                        if ((pieceBitboard & (1UL << i)) != 0)
                        {
                            int rank = i / 8;
                            int file = i % 8;

                            bitboard[dimension, rank, file] = 1;
                        }
                    }

                    // Store attacking squares in the appropriate dimension
                    Move[] legalAttackingMoves = board.GetLegalMoves(true);
                    foreach (Move attack in legalAttackingMoves)
                    {
                        bitboard[dimension + 12, attack.TargetSquare.Rank, attack.TargetSquare.File] = 1;
                    }
                }
            }
            
            return bitboard;
        }

        static float[,,] FillFirstDim(float[,,] bitboard, int dim, int value)
        {
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    bitboard[dim, y, x] = value;

            return bitboard;
        }


        public class Convolution2DLayer
        {
            private float[,] filter;
            private int padding;
            private int stride;

            public Convolution2DLayer(float[,] filter, int inputChannels, int outputChannels, int padding, int stride)
            {
                // Initialize filters randomly or with specific values
                this.filter = filter;
                this.padding = padding;
                this.stride = stride;
            }

            public float[,,] Forward(float[,,] input)
            {
                int inputChannels = input.GetLength(0);
                int inputHeight = input.GetLength(1);
                int inputWidth = input.GetLength(2);
                int filterSize = filter.GetLength(1);
                int outputChannels = filter.GetLength(0);

                int outputHeight = (inputHeight - filterSize + 2 * padding) / stride + 1;
                int outputWidth = (inputWidth - filterSize + 2 * padding) / stride + 1;

                float[,,] output = new float[outputChannels, outputHeight, outputWidth];

                for (int oc = 0; oc < outputChannels; oc++)
                    for (int y = 0; y < outputHeight; y++)
                        for (int x = 0; x < outputWidth; x++)
                            for (int ic = 0; ic < inputChannels; ic++)
                                for (int fy = 0; fy < filterSize; fy++)
                                    for (int fx = 0; fx < filterSize; fx++)
                                    {
                                        int inputY = y * stride + fy - padding;
                                        int inputX = x * stride + fx - padding;

                                        if (inputX >= 0 && inputX < inputWidth && inputY >= 0 && inputY < inputHeight)
                                            output[oc, y, x] += input[ic, inputY, inputX] * filter[fy, fx];
                                    }

                return output;
            }
        }



        public class FullyConnectedLayer
        {
            private float[,] weights;
            private float[] biases;

            public FullyConnectedLayer(int inputSize, int outputSize)
            {
                // Initialize weights and biases randomly or with specific values
                weights = new float[inputSize, outputSize];
                biases = new float[outputSize];
            }

            public float[] Forward(float[] input)
            {
                int inputSize = weights.GetLength(0);
                int outputSize = weights.GetLength(1);

                if (input.Length != inputSize)
                {
                    throw new ArgumentException("Input size does not match the layer's input size.");
                }

                float[] output = new float[outputSize];

                // Perform matrix multiplication between input and weights
                for (int i = 0; i < outputSize; i++)
                {
                    float sum = 0;
                    for (int j = 0; j < inputSize; j++)
                    {
                        sum += input[j] * weights[j, i];
                    }
                    output[i] = sum + biases[i];
                }

                return output;
            }
        }

        public class BatchNorm2DLayer
        {
            private float[] scales;
            private float[] biases;
            private float[] mean;
            private float[] variance;
            private float epsilon = 1e-5F;

            public BatchNorm2DLayer(int channels)
            {
                // Initialize scales, biases, mean, and variance (can be done randomly or with specific values)
                scales = new float[channels];
                biases = new float[channels];
                mean = new float[channels];
                variance = new float[channels];
            }

            public float[,,] Forward(float[,,] input)
            {
                int channels = input.GetLength(0);
                int height = input.GetLength(1);
                int width = input.GetLength(2);

                float[,,] output = new float[channels, height, width];

                // Normalize the input data using Batch Normalization formula
                for (int c = 0; c < channels; c++)
                {
                    float scale = scales[c];
                    float bias = biases[c];
                    float m = mean[c];
                    float v = variance[c];

                    for (int h = 0; h < height; h++)
                    {
                        for (int w = 0; w < width; w++)
                        {
                            float x = input[c, h, w];
                            float normalized = (x - m) / Math.Sqrt(v + epsilon);
                            output[c, h, w] = scale * normalized + bias;
                        }
                    }
                }

                return output;
            }
        }
        */
    }
}