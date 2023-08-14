using ChessChallenge.API;
using System;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace ChessChallenge.Example
{
    public class MyBot_V2 : IChessBot
    {
        bool botIsWhite = true;
        Move rootMove;
        int maxDepth = 3;
        int phase = 24;
        int LARGEVAL = 50000;
        int max;

        /*
        // Transposition table stuff
        private const sbyte EXACT = 0, LOWERBOUND = -1, UPPERBOUND = 1, INVALID = -2;
        //14 bytes per entry, likely will align to 16 bytes due to padding (if it aligns to 32, recalculate max TP table size)
        public struct Transposition
        {
            public Transposition(ulong zHash, int eval, byte d)
            {
                zobristHash = zHash;
                evaluation = eval;
                depth = d;
                flag = INVALID;
            }

            public ulong zobristHash = 0;
            public float evaluation = 0;
            public byte depth = 0;
            public sbyte flag = INVALID;
        };

        private static ulong k_TpMask = 0x7FFFFF; //9.4 million entries, likely consuming about 151 MB
        private Transposition[] m_TPTable = new Transposition[k_TpMask + 1];
        //To access
        Transposition Lookup(ulong zHash)
        {
            return m_TPTable[zHash & k_TpMask];
        }
        */

        // TODO: 
        // implement robust stalemate, repetition and 50 move rule detection to prevent drawing
        // check checkmate check in Evaluation
        // add iterative deepening!!

        public Move Think(Board board, Timer timer)
        {
            botIsWhite = board.IsWhiteToMove;
            rootMove = board.GetLegalMoves()[0]; // To avoid Null moves
            int alpha = -LARGEVAL;
            int beta = LARGEVAL;
            max = -LARGEVAL;
            phase = ComputePhase(board);

            int score = NegaMax(board, maxDepth, 0, alpha, beta, 1);
            Console.WriteLine(score);

            return rootMove;
        }

        int NegaMax(Board board, int depth, int ply, int alpha, int beta, int colour)
        {
            int origAlpha = alpha;

            /*
            Transposition ttEntry = Lookup(board.ZobristKey);
            if (ttEntry.flag != INVALID && ttEntry.depth >= depth)
            {
                if (ttEntry.flag == EXACT) return ttEntry.evaluation;
                if (ttEntry.flag == LOWERBOUND) alpha = Math.Max(alpha, ttEntry.evaluation);
                if (ttEntry.flag == UPPERBOUND) beta = Math.Min(beta, ttEntry.evaluation);
            }

            */
            if (ply > 0 && board.IsRepeatedPosition())
            {
                return -5;
            }
            
            if (depth == 0)
            {
                return Quiesce(alpha, beta, board, ply, colour);
            }

            Move[] legalMoves = board.GetLegalMoves();
            foreach (Move move in legalMoves)
            {
                // TODO: ORDER MOVES
                board.MakeMove(move);
                int score = -NegaMax(board, depth - 1, ply + 1, -beta, -alpha, -colour);
                board.UndoMove(move);

                if (score >= beta)
                {
                    return beta; // Fail hard beta cut-off
                }
                if (score > alpha)
                {
                    // Console.WriteLine("New alpha " + alpha);
                    alpha = score; // Alpha is max
                    if (depth == maxDepth)
                        rootMove = move;
                }

                /*
                ttEntry.evaluation = score;
                if (alpha <= origAlpha) ttEntry.flag = UPPERBOUND;
                else if (alpha >= beta) ttEntry.flag = LOWERBOUND;
                else ttEntry.flag = EXACT;
                ttEntry.depth = (byte)depth;
                m_TPTable[board.ZobristKey & 0x7FFFFF] = ttEntry;
                */
            }

            return alpha;
        }

        int Quiesce(int alpha, int beta, Board board, int ply, int colour)
        {
            int stand_pat = Evaluate(board, ply, colour);
            if (stand_pat >= beta)
                return beta;
            if (alpha < stand_pat)
                alpha = stand_pat;

            Move[] captures = board.GetLegalMoves(true);
            foreach(Move capture in captures) {
                board.MakeMove(capture);
                int score = -Quiesce(-beta, -alpha, board, ply, -colour);
                board.UndoMove(capture);

                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
            return alpha;
        }

        // PeSTO Evaluation Function
        readonly int[] pvm_mg = { 0, 82, 337, 365, 477, 1025, 20000 };
        readonly int[] pvm_eg = { 0, 94, 281, 297, 512, 936, 20000 };

        // thanks for the compressed pst implementation https://github.com/JacquesRW
        readonly ulong[] pst_compressed = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

        public int Get_Pst_Bonus(int psq)
        {
            return (int)(((pst_compressed[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
        }

        // Eval function using PeSTO and Tapered Eval
        int Evaluate(Board board, int ply, int colour)
        {
            int turn = Convert.ToInt32(board.IsWhiteToMove);
            int[] scoreMiddleGame = { 0, 0 };
            int[] scoreEndGame = { 0, 0 };

            foreach (bool side in new[] { false, true })
            {
                for (int piece_type = 1; piece_type <= 6; piece_type++)
                {
                    ulong bb = board.GetPieceBitboard((PieceType)piece_type, side);
                    while (bb > 0)
                    {
                        // Crazy ulong compressed computations from https://github.com/JacquesRW
                        int index = 128 * (piece_type - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (side ? 56 : 0);
                        scoreMiddleGame[Convert.ToInt32(side)] += pvm_mg[piece_type] + Get_Pst_Bonus(index);
                        scoreEndGame[Convert.ToInt32(side)] += pvm_eg[piece_type] + Get_Pst_Bonus(index + 64);
                    }
                }
            }

            if (board.IsInCheckmate()) {
                Console.WriteLine("Found mate in " + ply + " for " + (colour == 1 ? "Bot " : "Opponent ") + colour * (LARGEVAL - ply));
                return colour * (LARGEVAL - ply);
            }

            // Tapered Eval
            return colour * (((scoreMiddleGame[turn] - scoreMiddleGame[1 ^ turn]) * (256 - phase)) + ((scoreEndGame[turn] - scoreEndGame[1 ^ turn]) * phase)) / 256;
        }

        // Compute phase of the game (opening -> ending).
        // Used for tapered eval.
        int ComputePhase(Board board)
        {
            int phase = 24;
            PieceType[] pieceTypesList = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
            int[] piecePhases = { 0, 1, 1, 2, 4 };

            for (int colour = 0; colour < 2; colour++)
            {
                for (int i = 0; i < 5; i++)
                {
                    PieceList pieceList = board.GetPieceList(pieceTypesList[i], Convert.ToBoolean(colour));
                    phase -= pieceList.Count * piecePhases[i];
                }
            }
            
            return phase;
        }
    }
}