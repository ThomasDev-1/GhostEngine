using System;
using ChessChallenge.API;
using System.Linq;
public class BettyBot_V5 : IChessBot
{
    #region Variables

    decimal[] packedPestoTables = {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
        2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
        77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m
        };

    int[][] unpackedPestoTables;
    short[] pieceValues = { 82, 337, 365, 477, 1025, 0, // Middlegame
                                             94, 281, 297, 512, 936, 0}; // Endgame
    Move rootBestMove = Move.NullMove;
    float timeLimit;
    Board board;
    Timer timer;

    record struct Transposition(ulong key, float score, int depth, int flag);
    const ulong transpositionTableEntries = 0x7FFFFF;
    Transposition[] transpositionTable = new Transposition[transpositionTableEntries];

    #endregion

    public BettyBot_V5()
    {
        unpackedPestoTables = packedPestoTables.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select(square => (int)((sbyte)square * 1.461) + pieceValues[pieceType++]))
                .ToArray();
        }).ToArray();
    }
    public Move Think(Board _board, Timer _timer)
    {
        timeLimit = (_timer.MillisecondsRemaining / 30) + _timer.IncrementMilliseconds;
        board = _board;
        timer = _timer;

        int depth = 1;
        Move bestMove = board.GetLegalMoves()[0];
        while (depth < 150)
        {
            depth++;
            float score = NegaMax(depth, 0, int.MinValue, int.MaxValue);
            if (timer.MillisecondsElapsedThisTurn >= timeLimit) break;
            bestMove = rootBestMove;
            if (score > 24999) break;

        }
        return bestMove;
    }

    //
    //Algorithms
    //
    #region Alg's
    float NegaMax(int depth, int plyToRoot, float alpha, float beta, bool doNullMove = true)
    {
        ulong zobristKey = board.ZobristKey;
        bool doQSearch = depth <= 0;
        float bestScore = float.MinValue, evaluation = Evaluate(), startAlpha = alpha;

        if (board.IsDraw() || board.IsRepeatedPosition()) return 0;
        if (!doQSearch && board.GetLegalMoves().Length == 0) return board.IsInCheck() ? -50000 + plyToRoot : 0;

        // PRUNING

        Transposition transposition = transpositionTable[zobristKey % transpositionTableEntries];
        if (transposition.key == zobristKey && plyToRoot > 0 && transposition.depth >= depth &&
                (transposition.flag == 1 ||
                (transposition.flag == 0 && transposition.score <= alpha) ||
                (transposition.flag == 2 && transposition.score >= beta)))
            return transposition.score;

        if (doQSearch)
        {
            bestScore = evaluation;
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
        }

        if (doNullMove && depth > 4 && board.TrySkipTurn())
        {
            float eval = -NegaMax(depth - 3, plyToRoot + 1, -beta, 1 - beta, false);
            board.UndoSkipTurn();

            if (eval >= beta)
                return eval;
        }

        // PRUNING END

        if (board.IsInCheck()) depth++;

        bool searchRaiseAlpha = true;
        foreach (Move move in OrderMoves(board.GetLegalMoves(doQSearch)))
        {
            board.MakeMove(move);
            float score = -NegaMax(depth - 1, plyToRoot + 1, searchRaiseAlpha ? -beta : -alpha - 1, -alpha);

            if (!searchRaiseAlpha && alpha < score && score < beta)
                score = -NegaMax(depth - 1, plyToRoot + 1, -beta, -alpha);

            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn > timeLimit) return 0;

            if (score > bestScore)
            {
                bestScore = score;
                alpha = Math.Max(alpha, bestScore);

                if (plyToRoot == 0) rootBestMove = move;
            }

            if (alpha >= beta) break;

            searchRaiseAlpha = false;

        }

        transpositionTable[zobristKey % transpositionTableEntries] = new Transposition(
            zobristKey,
            bestScore,
            depth,
            bestScore >= beta ? 2 : bestScore > startAlpha ? 1 : 0
        );
        return alpha; //return bestScore;
    }
    Move[] OrderMoves(Move[] moves)
    {
        int evaluateMove(Board board, Move move)
        {
            if (move.IsPromotion) return 5;
            if (move.IsCapture)
                return (int)board.GetPiece(move.TargetSquare).PieceType * 20 -
                    (int)board.GetPiece(move.StartSquare).PieceType;
            return 0;

        }

        moves.OrderByDescending(move => evaluateMove(board, move));
        return moves;
    }

    #endregion

    //
    //Evaluation code
    //
    #region Eval

    private float Evaluate()
    {
        // index 0: whitescore
        // index 1: blackscore

        float middleGame = 0, endGame = 0;
        // 0: white, 1: black
        foreach (bool isWhite in new bool[] { true, false })
        {
            // 1: pawn, 2: knight ...
            for (int type = 1; type < 7; type++)
            {
                // returns a 64 bit, 1 represents a piece on an index
                ulong bitboard = board.GetPieceBitboard((PieceType)type, isWhite);

                // If there is more than one bit set to one and we check bishops, handle the bishop pair bonus
                // Adds 30 tokens
                if (type == 3 &&
                    System.Numerics.BitOperations.PopCount(bitboard) > 1)
                {
                    middleGame += 30;
                    endGame += 50;
                }

                // while pieces left on the bitboard
                while (bitboard > 0)
                {
                    // convert bitboard into square index
                    int index = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
                    // convert index for white pieces horizontally
                    if (isWhite) index = index ^ 56;
                    // add score

                    middleGame += unpackedPestoTables[index][type - 1];
                    endGame += unpackedPestoTables[index][type + 5];
                }
            }
            middleGame = -middleGame;
            endGame = -endGame;
        }

        return (middleGame * (1 - EndgameTransition(board)) +
            endGame * EndgameTransition(board)) * (board.IsWhiteToMove ? 1 : -1);
    }
    float EndgameTransition(Board board) => 1 - (board.GetAllPieceLists().Sum(pieceList => pieceList.Sum(piece => pieceValues[(int)piece.PieceType - 1])) / 8078f);
    #endregion

}