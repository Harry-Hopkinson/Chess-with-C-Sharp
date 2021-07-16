using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    public class AI
    {
        public static int DEPTH = 4;
        public static bool RUNNING = false;
        public static bool STOP = false;
        private static Player MAX = Player.BLACK;

        public static move_t MiniMaxAB(ChessBoard board, Player turn)
        {
            RUNNING = true;
            STOP = false;
            MAX = turn;

            // gather all possible moves
            Dictionary<position_t, List<position_t>> moves = LegalMoveSet.getPlayerMoves(board, turn);

            // because we're threading safely store best result from each thread
            int[] bestresults = new int[moves.Count];
            move_t[] bestmoves = new move_t[moves.Count];

            // thread the generation of each move
            Parallel.ForEach(moves, (movelist,state,index) =>
            {
                if (STOP) // interupt
                {
                    state.Stop();
                    return;
                }

                // initialize thread best
                bestresults[index] = int.MinValue;
                bestmoves[index] = new move_t(new position_t(-1, -1), new position_t(-1, -1));

                // for each move for the current piece(thread)
                foreach (position_t move in movelist.Value)
                {
                    if (STOP)
                    {
                        state.Stop();
                        return;
                    }

                    
                    ChessBoard b2 = LegalMoveSet.move(board, new move_t(movelist.Key, move));
                    int result = mimaab(b2, (turn == Player.WHITE) ? Player.BLACK : Player.WHITE, 1, Int32.MinValue, Int32.MaxValue);

                    
                    if (bestresults[index] < result || (bestmoves[index].to.Equals(new position_t(-1, -1)) && bestresults[index] == int.MinValue))
                    {
                        bestresults[index] = result;
                        bestmoves[index].from = movelist.Key;
                        bestmoves[index].to = move;
                    }
                }
            });

            if (STOP)
                return new move_t(new position_t(-1, -1), new position_t(-1, -1)); 

            int best = int.MinValue;
            move_t m = new move_t(new position_t(-1, -1), new position_t(-1, -1));
            for(int i = 0; i < bestmoves.Length; i++)
            {
                if (best < bestresults[i] || (m.to.Equals(new position_t(-1,-1)) && !bestmoves[i].to.Equals(new position_t(-1,-1))))
                {
                    best = bestresults[i];
                    m = bestmoves[i];
                }
            }
            return m;
        }

        private static int mimaab(ChessBoard board, Player turn, int depth, int alpha, int beta)
        {
            if (depth >= DEPTH)
                return board.fitness(MAX);
            else
            {
                List<ChessBoard> boards = new List<ChessBoard>();

                // get available moves / board states from moves for the current player
                foreach (position_t pos in board.Pieces[turn])
                {
                    if (STOP) return -1; // interupts
                    List<position_t> moves = LegalMoveSet.getLegalMove(board, pos);
                    foreach (position_t move in moves)
                    {
                        if (STOP) return -1; // interupts
                        ChessBoard b2 = LegalMoveSet.move(board, new move_t(pos, move));
                        boards.Add(b2);
                    }
                }

                int a = alpha, b = beta;
                if (turn != MAX) // minimize
                {
                    foreach (ChessBoard b2 in boards)
                    {
                        if (STOP) return -1; // interupt
                        b = Math.Min(b, mimaab(b2, (turn == Player.WHITE) ? Player.BLACK : Player.WHITE, depth + 1, a, b));
                        if (a >= b)
                            return a;
                    }
                    return b;
                }
                else // maximize
                {
                    foreach (ChessBoard b2 in boards)
                    {
                        if (STOP) return -1; // interupt
                        a = Math.Max(a, mimaab(b2, (turn == Player.WHITE) ? Player.BLACK : Player.WHITE, depth + 1, a, b));
                        if (a >= b)
                            return b;
                    }
                    return a;
                }
            }
        }
    }
}
