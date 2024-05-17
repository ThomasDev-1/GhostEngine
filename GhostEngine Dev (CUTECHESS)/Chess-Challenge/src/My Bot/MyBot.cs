using ChessChallenge.API;
using System;
using System.Diagnostics;
using System.Linq;

public class MyBot : IChessBot
{
    public readonly decimal[] PackedPestoTables = {
    63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
    77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
    2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
    77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
    75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
    75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
    73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
    68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
    };

    public readonly int[][] UnpackedPestoTables;

    public MyBot()
    {
        UnpackedPestoTables = new int[64][];
        UnpackedPestoTables = PackedPestoTables.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + PieceValues[pieceType++]))
                .ToArray();
        }).ToArray();
    }

    public bool OutOfTime => searchTimer.MillisecondsElapsedThisTurn > maxTimePerMove;
    public int maxTimePerMove;
    public Timer searchTimer;
    public Board board;
    public Move rootMove;
    readonly Move[] kMoves = new Move[1024];
    readonly int[,,] hMoves = new int[2, 7, 64];
    (ulong, Move, int, int, int)[] TTtable = new (ulong, Move, int, int, int)[1048576];

    public Move Think(Board newBoard, Timer timer)
    {
        board = newBoard;
        searchTimer = timer;

        maxTimePerMove = timer.MillisecondsRemaining / 50;

        for (int depth = 2; ;)
        {
            NMSearch(depth++, -9999999, 9999999, 0);

            if (OutOfTime) { Console.WriteLine("Final: " + depth); return rootMove;};
            Console.WriteLine(depth);
        }
    }

    int NMSearch(int depth, int alpha, int beta, int ply)
    {
        bool isNotRoot = ply > 0, qsearch = depth <= 0, InCheck = board.IsInCheck(), notPvNode = alpha + 1 == beta;
        ulong key = board.ZobristKey;
        int staticEval = Evaluate();

        if (isNotRoot && board.IsRepeatedPosition()) return -staticEval;

        var (ttKey, ttMove, ttDepth, ttScore, ttBound) = TTtable[key % 1048576];

        // TT cutoff
        if (Math.Abs(ttScore) < 30000 - 1000 && isNotRoot && ttKey == key && ttDepth >= depth && (
            ttBound == 3 // exact score
                || ttBound == 2 && ttScore >= beta // lower bound, fail high
                || ttBound == 1 && ttScore <= alpha // upper bound, fail low
        )) return ttScore;



        int bestScore = -600001;

        //QSearching
        if (qsearch)
        {
            if (staticEval >= beta) return staticEval;
            alpha = Math.Max(alpha, staticEval);
        }
        else if (notPvNode && depth <= 4)
        {
            //Reverse Futility Pruning
            if (staticEval - 135 * depth >= beta) return staticEval;
        }

        // Null move pruning
        if (notPvNode && depth >= 4 && board.TrySkipTurn())
        {
            int score = -NMSearch(depth - 3 - depth / 5, -beta, -beta + 1, ply + 1);
            board.UndoSkipTurn();
            if (score >= beta) return score;
        }

        var allMoves = board.GetLegalMoves(qsearch);
        int amtMoves = allMoves.Length, origAlpha = alpha, colour = board.IsWhiteToMove ? 0 : 1, loop = 0;

        var scores = new int[amtMoves];

        // Move ordering       
        for (int i = 0; i < amtMoves; i++)
        {
            Move move = allMoves[i];
            // TT move
            scores[i] = -(key == ttKey && ttMove == move ? 1000000000 :
                            move.IsCapture ? 100000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                            move.IsPromotion ? 100000 * (int)move.PromotionPieceType :
                            kMoves[ply] == move ? 95000 :
                            hMoves[colour, (int)move.MovePieceType, move.TargetSquare.Index]);
        }

        //while (loop < amtMoves)
        //{
        //    Move move = allMoves[loop++];
        //    // TT move
        //    scores[loop] = -(key == ttKey && ttMove == move ? 1000000000 :
        //                    move.IsCapture ? 100000 * (int)move.CapturePieceType - (int)move.MovePieceType :
        //                    move.IsPromotion ? 100000 * (int)move.PromotionPieceType :
        //                    kMoves[ply] == move ? 95000 :
        //                    hMoves[colour, (int)move.MovePieceType, move.TargetSquare.Index]);
        //}


        Move bestMove = Move.NullMove;
        Array.Sort(scores, allMoves);
        if (InCheck) depth++;

        for (int movesTried = 0; movesTried < amtMoves; movesTried++)
        {
            if (OutOfTime) return 999999;

            Move move = allMoves[movesTried];


            //LMP
            if (movesTried > 3 + depth * depth && !qsearch && depth <= 4 && scores[movesTried] > 95000) break;

            board.MakeMove(move);
            int score;

            if (movesTried == 0 || qsearch) goto PV;

            //Late Move Reduction
            int reduction = movesTried > 3 && depth > 3 && !(move.IsCapture ||
                board.IsInCheck()) ? 1 + movesTried / (notPvNode ? 6 : 8) + depth / 6 : 1;

            score = -NMSearch(depth - reduction, -alpha - 1, -alpha, ply + 1);

            //Razoring
            if (!(score > alpha && (reduction > 1 || beta > score))) goto END;

            //PV Searching
            PV:
            score = -NMSearch(depth - 1, -beta, -alpha, ply + 1);

            END:
            board.UndoMove(move);


            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (!isNotRoot) rootMove = move;
                alpha = Math.Max(alpha, bestScore);
            }
            
            //Alpha Beta Pruning
            if (alpha >= beta)
            {
                if (!move.IsCapture)
                {
                    kMoves[ply] = move;
                    hMoves[colour, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                }
                break;
            }


        }

        if (!qsearch && amtMoves == 0)
            return InCheck ? -30000 + ply : 0;

        TTtable[key % 1048576] = (key,
                                 bestMove,
                                 depth,
                                 bestScore,
                                 bestScore >= beta ? 2 : bestScore > origAlpha ? 3 : 1);

        return bestScore;

    }

    public readonly int[] GamePhaseIncrement = { 0, 1, 1, 2, 4, 0 };

    public readonly short[] PieceValues = { 82, 337, 365, 477, 1025, 0, // Middlegame
                                        94, 281, 297, 512, 936, 0}; // Endgame

    public int Evaluate()
    {
        int middlegame = 0, endgame = 0, gamephase = 0;
        foreach (bool sideToMove in new[] { true, false })
        {
            ulong mask = board.GetPieceBitboard(PieceType.Pawn, sideToMove);

            for (int piece = 0, square; piece < 5; mask = board.GetPieceBitboard((PieceType)(++piece + 1), sideToMove))
                while (mask != 0)
                {
                    gamephase += GamePhaseIncrement[piece];
                    square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (sideToMove ? 56 : 0);
                    middlegame += UnpackedPestoTables[square][piece];
                    endgame += UnpackedPestoTables[square][piece + 6];
                }

            middlegame = -middlegame;
            endgame = -endgame;
        }
        return (middlegame * gamephase + endgame * (24 - gamephase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

}