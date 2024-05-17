using System;
using ChessChallenge.API;
using System.Collections.Generic;

public class BettyBot_V3 : IChessBot
{
    public Move bestMove;

    public Move Think(Board board, Timer timer)
    {
        for (int depth = 1; depth <= 50; depth++)
        {
            AlphaBetaMinimax(board, depth, int.MinValue, int.MaxValue, board.IsWhiteToMove, timer, 0);

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 40)
                return bestMove;
        }
        return bestMove;
    }

    public int AlphaBetaMinimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, Timer timer, int plyToRoot)
    {
        if (depth == 0)
        {
            int score = EvalBoard(board);
            return score;
        }

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                return -10000;
            else
                return 10000;
        }

        if (board.IsDraw())
        {
            if (board.IsWhiteToMove)
                return 10000;
            else
                return -10000;
        }

        Move[] legalMoves = OrderMoves(board);

        if (maximizingPlayer)
        {
            int bestScore = int.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, false, timer, plyToRoot + 1);
                board.UndoMove(move);
                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 40)
                {
                    return EvalBoard(board);
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
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, true, timer, plyToRoot + 1);
                board.UndoMove(move);

                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 40)
                {
                    return EvalBoard(board);
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
            return bestScore;
        }
    }

    private static Move[] OrderMoves(Board board)
    {
        List<Move> captures = new();
        List<Move> otherMoves = new();

        captures.AddRange(board.GetLegalMoves(true));
        otherMoves.AddRange(board.GetLegalMoves(false));

        otherMoves.RemoveRange(0, captures.Count);

        List<Move> orderedMoves = new();
        orderedMoves.AddRange(captures);
        orderedMoves.AddRange(otherMoves);

        return orderedMoves.ToArray();
    }

    private int EvalBoard(Board board)
    {
        const int pawnValue = 100;
        const int rookValue = 500;
        const int knightValue = 320;
        const int bishopValue = 330;
        const int queenValue = 900;
        const int kingValue = 99999;

        int whiteEval = 0, blackEval = 0;
        int squereBonus = 0;
        foreach (PieceList pList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pList)
                squereBonus += GetSquareBonus(piece.PieceType, piece.IsWhite, piece.Square.File, piece.Square.Rank);
        }

        foreach (Piece piece in board.GetPieceList(PieceType.Pawn, true))
            whiteEval += pawnValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Knight, true))
            whiteEval += knightValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Bishop, true))
            whiteEval += bishopValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Rook, true))
            whiteEval += rookValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Queen, true))
            whiteEval += queenValue;

        foreach (Piece piece in board.GetPieceList(PieceType.Pawn, false))
            blackEval += pawnValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Knight, false))
            blackEval += knightValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Bishop, false))
            blackEval += bishopValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Rook, false))
            blackEval += rookValue;
        foreach (Piece piece in board.GetPieceList(PieceType.Queen, false))
            blackEval += queenValue;

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                whiteEval += kingValue;
            else
                blackEval += kingValue;
        }

        return (whiteEval - blackEval + squereBonus);
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