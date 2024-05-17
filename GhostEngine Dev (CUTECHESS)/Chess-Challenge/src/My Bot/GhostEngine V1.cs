using System;
using ChessChallenge.API;
using System.Linq;

public class GhostEngine_V1 : IChessBot
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

    public GhostEngine_V1()
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

    public Move Think(Board newBoard, Timer timer)
    {
        board = newBoard;
        searchTimer = timer;

        historyHeuristics = new int[2, 7, 64];

        maxTimePerMove = timer.MillisecondsRemaining / 30;

        for (int depth = 2; ;)
        {
            NMSearch(depth++, -9999999, 9999999, 0);

            if (OutOfTime || depth > 50) return rootMove;
        }
    }

    public int NMSearch(int depth, int alpha, int beta, int searchPly, bool allowNull = true)
    {
        bool inCheck = board.IsInCheck(),
            isPV = beta - alpha > 1,
            notRoot = searchPly > 0;

        if (notRoot && board.IsRepeatedPosition()) return 0;

        int bestEval = -9999999,
            originalAlpha = alpha,
            movesTried = 0,
            nextDepth = depth - 1,
            eval = Evaluate();

        TTEntry entry = transpositionTable[board.ZobristKey & 0x3FFFFF];
        if (entry.Hash == board.ZobristKey && notRoot &&
            entry.Depth >= depth)
        {
            int score = entry.Score;
            if (entry.Flag == 1) return score;
            if (entry.Flag == 3) alpha = Math.Max(alpha, score);
            else beta = Math.Min(beta, score);

            if (alpha >= beta) return score;
        }


        bool inQSearch = depth <= 0;

        if (!inQSearch && inCheck) depth++;
        //QSeraching
        if (inQSearch)
        {
            bestEval = eval;

            alpha = Math.Max(alpha, bestEval);
            if (alpha >= beta)
                return bestEval;
        }
        else if (!isPV && !inCheck)
        {
            //Reverse futility pruning
            if (eval - 100 * depth >= beta)
                return eval - 100 * depth;

            // Null move pruning
            if (allowNull)
            {
                board.TrySkipTurn();
                eval = -NMSearch(depth - 3 - depth / 5, -beta, 1 - beta, searchPly, false);
                board.UndoSkipTurn();

                if (eval >= beta)
                    return eval;
            }

        }

        var moves = board.GetLegalMoves(inQSearch && !inCheck)?.OrderByDescending(move =>
        {
            return move == entry.BestMove ? 100000 :
            // MVVLVA
            move.IsCapture ? 1000 * (int)move.CapturePieceType - (int)move.MovePieceType :
            historyHeuristics[board.IsWhiteToMove ? 1 : 0, (int)move.MovePieceType, move.TargetSquare.Index];
        }).ToArray();

        if (!inQSearch && moves.Length == 0) return inCheck ? searchPly - 99999 : 0;

        Move bestMove = default;
        foreach (Move move in moves)
        {
            if (OutOfTime) break; // return 0;
            bool tactical = movesTried == 0 || move.IsCapture || move.IsPromotion;

            //LMP
            if (beta - alpha == 1 && !inCheck && !tactical && movesTried > 2 + depth * depth) break;

            board.MakeMove(move);

            int Search(int newDepth, int newAlpha) => -NMSearch(newDepth, -newAlpha, -alpha, searchPly + 1, allowNull);

            // LMR + PVS
            if (movesTried++ == 0 || inQSearch) eval = Search(nextDepth, beta);

            else if ((eval = isPV || tactical || movesTried < 6 || depth < 3 || inCheck || board.IsInCheck()
                    ? alpha + 1
                    : Search(nextDepth - depth / 3, alpha + 1)) > alpha &&

                    alpha < (eval = Search(nextDepth, alpha + 1)) && eval < beta)
                eval = Search(nextDepth, beta);

            board.UndoMove(move);

            if (eval > bestEval)
            {
                bestMove = move;
                bestEval = eval;

                if (!notRoot && moves.Contains(move)) rootMove = move;

                alpha = Math.Max(eval, alpha);

                if (alpha >= beta)
                {
                    if (!move.IsCapture)
                        historyHeuristics[board.IsWhiteToMove ? 1 : 0, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                    break;
                }
            }
        }

        transpositionTable[board.ZobristKey & 0x3FFFFF] = new TTEntry(
            board.ZobristKey,
            bestMove,
            bestEval,
            depth,
            bestEval >= beta ? 3 : bestEval <= originalAlpha ? 2 : 1);

        return alpha;
    }
    int[,,] historyHeuristics;

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

    public readonly TTEntry[] transpositionTable = new TTEntry[0x400000];
    public record struct TTEntry(ulong Hash, Move BestMove, int Score, int Depth, int Flag);
}