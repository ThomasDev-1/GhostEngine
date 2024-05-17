using System;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class BettyBot_V4 : IChessBot
{
    private Dictionary<ulong, (int score, Move move)> transpositionTable;
    public Move pubBestMove;
    public int estNumMoves = 50;

    public BettyBot_V4() => transpositionTable = new Dictionary<ulong, (int score, Move move)>();

    public Move Think(Board board, Timer timer)
    {
        pubBestMove = board.GetLegalMoves()[0];
        for (int depth = 2; depth <= 50; depth++)
        {
            var result = AlphaBetaMinimax(board, depth, int.MinValue, int.MaxValue, board.IsWhiteToMove, timer, 0);

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / estNumMoves)
                return pubBestMove;
            pubBestMove = result.move;
        }
        return pubBestMove;
    }

    public (int score, Move move) AlphaBetaMinimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, Timer timer, int plyToRoot)
    {
        if (transpositionTable.TryGetValue(board.ZobristKey, out var ttEntry) && ttEntry.move != Move.NullMove && ttEntry.score != int.MinValue)
        {
            if (ttEntry.move != Move.NullMove && ttEntry.score != int.MinValue && depth <= 0)
                return ttEntry;
        }
        if (depth == 0)
        {
            int score = EvalBoard(board);
            return (score, pubBestMove);
        }

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                return (-10000, pubBestMove);
            else
                return (10000, pubBestMove);
        }

        if (board.IsDraw())
        {
            return (-EvalBoard(board), pubBestMove);
        }

        Move[] legalMoves = OrderMoves(board);
        Move bestMove = legalMoves[0];

        if (maximizingPlayer)
        {
            int bestScore = int.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, false, timer, plyToRoot + 1).score;
                board.UndoMove(move);

                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / estNumMoves)
                {
                    int pScore = EvalBoard(board);
                    return (score, bestMove);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    if (plyToRoot == 0)
                    {
                        bestMove = move;
                    }
                }

                alpha = Math.Max(alpha, bestScore);
                if (alpha >= beta)
                    break;
            }

            transpositionTable[board.ZobristKey] = (bestScore, bestMove);
            return (bestScore, bestMove);
        }
        else
        {
            int bestScore = int.MaxValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, true, timer, plyToRoot + 1).score;
                board.UndoMove(move);

                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / estNumMoves)
                {
                    int pScore = EvalBoard(board);
                    return (score, bestMove);
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    if (plyToRoot == 0)
                    {
                        bestMove = move;
                    }
                }

                beta = Math.Min(beta, bestScore);
                if (alpha >= beta)
                    break;
            }

            transpositionTable[board.ZobristKey] = (bestScore, bestMove);
            return (bestScore, bestMove);
        }
    }

    private Move[] OrderMoves(Board board)
    {
        List<Move> checks = new List<Move>();
        List<Move> captures = new List<Move>();
        List<Move> otherMoves = new List<Move>();

        Move[] legalMoves = board.GetLegalMoves();

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            if (board.IsInCheck())
            {
                checks.Add(move);
            }
            else if (move.IsCapture)
            {
                captures.Add(move);
            }
            else
            {
                otherMoves.Add(move);
            }
            board.UndoMove(move);
        }

        List<Move> orderedMoves = new List<Move>();
        orderedMoves.AddRange(checks);
        orderedMoves.AddRange(captures);
        orderedMoves.AddRange(otherMoves);

        return orderedMoves.ToArray();
    }

    private int EvalBoard(Board board)
    {
        int[] pieceValues = { 0, 82, 337, 365, 477, 1025, 0 };
        int mateScore = 99999;

        int whiteEval = 0, blackEval = 0;
        int squereBonus = 0;
        int whiteMobility = 0, blackMobility = 0;

        foreach (Move move in board.GetLegalMoves())
        {
            if (board.GetPiece(move.StartSquare).IsWhite)
                whiteMobility += 1;
            else
                blackMobility += 1;

        }

        foreach (PieceList pList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pList)
            {
                squereBonus += GetSquareBonus(piece.PieceType, piece.IsWhite, piece.Square.File, piece.Square.Rank);
                if (piece.IsWhite)
                    whiteEval += pieceValues[(int)piece.PieceType];
                else
                    blackEval += pieceValues[(int)piece.PieceType];
            }
        }

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                whiteEval += mateScore;
            else
                blackEval += mateScore;
        }

        return (whiteEval - blackEval) + (squereBonus) + (whiteMobility - blackMobility);
    }

    private readonly ulong[,] PackedEvaluationTables = {
        { 58233348458073600, 61037146059233280, 63851895826342400, 66655671952007680 },
        { 63862891026503730, 66665589183147058, 69480338950193202, 226499563094066 },
        { 63862895153701386, 69480338782421002, 5867015520979476,  8670770172137246 },
        { 63862916628537861, 69480338782749957, 8681765288087306,  11485519939245081 },
        { 63872833708024320, 69491333898698752, 8692760404692736,  11496515055522836 },
        { 63884885386256901, 69502350490469883, 5889005753862902,  8703755520970496 },
        { 63636395758376965, 63635334969551882, 21474836490,       1516 },
        { 58006849062751744, 63647386663573504, 63625396431020544, 63614422789579264 }
    };

    public int GetSquareBonus(PieceType type, bool isWhite, int file, int rank)
    {
        if (file > 3)
            file = 7 - file;

        if (isWhite)
            rank = 7 - rank;

        sbyte unpackedData = unchecked((sbyte)((PackedEvaluationTables[rank, file] >> 8 * ((int)type - 1)) & 0xFF));

        return isWhite ? unpackedData : -unpackedData;
    }
}