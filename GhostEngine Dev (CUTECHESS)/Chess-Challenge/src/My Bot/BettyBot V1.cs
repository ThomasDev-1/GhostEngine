using System;
using ChessChallenge.API;
using System.Collections.Generic;

public class BettyBot_V1 : IChessBot
{
    private const int DefaultSearchDepth = 4;
    private int searchDepth;
    private Dictionary<ulong, (int score, Move move)> transpositionTable;

    public BettyBot_V1()
    {
        searchDepth = DefaultSearchDepth;
        transpositionTable = new Dictionary<ulong, (int score, Move move)>();
    }

    public Move Think(Board board, Timer timer)
    {
        transpositionTable.Clear();
        int totalMoves = 200;
        int movesLeft = totalMoves - (board.PlyCount);
        int timeLimitPerMove = (int)(timer.MillisecondsRemaining / movesLeft) - 500;
        int currentDepth = 1;
        Move bestMove = Move.NullMove;

        while (currentDepth <= searchDepth && timer.MillisecondsRemaining > timeLimitPerMove)
        {
            if (board.GetAllPieceLists().Length < 10)
            {
                searchDepth = 16;
            }

            Move move = AlphaBetaMinimax(board, currentDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove).move;

            if (move == Move.NullMove)
            {
                return bestMove;
            }
            else
            {
                bestMove = move;
            }
            currentDepth++;
        }
        Console.WriteLine("V1: " + currentDepth);
        return bestMove;
    }

    private (int score, Move move) AlphaBetaMinimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (transpositionTable.TryGetValue(board.ZobristKey, out var ttEntry) && ttEntry.move != Move.NullMove && ttEntry.score != int.MinValue)
        {
            if (ttEntry.move != Move.NullMove && ttEntry.score != int.MinValue && depth <= 0)
            {
                return ttEntry;
            }
        }

        if (depth == 0)
        {
            int score = EvalBoard(board);
            return (score, Move.NullMove);
        }

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
            {
                return (-10000, Move.NullMove);
            }
            else
            {
                return (10000, Move.NullMove);
            }
        }

        if (board.IsDraw())
        {
            if (board.IsWhiteToMove)
            {
                return (-8000, Move.NullMove);
            }
            else
            {
                return (8000, Move.NullMove);
            }
        }

        Move[] legalMoves = board.GetLegalMoves();
        Move bestMove = Move.NullMove;

        if (maximizingPlayer)
        {
            int bestScore = int.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, false).score;
                board.UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, bestScore);
                if (alpha >= beta)
                {
                    break;
                }
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
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, true).score;
                board.UndoMove(move);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                beta = Math.Min(beta, bestScore);
                if (alpha >= beta)
                {
                    break;
                }
            }

            transpositionTable[board.ZobristKey] = (bestScore, bestMove);
            return (bestScore, bestMove);
        }
    }

    private int EvalBoard(Board board)
    {
        const float pawnValue = 1;
        const float rookValue = 5;
        const float knightValue = 3;
        const float bishopValue = 3;
        const float queenValue = 9;
        const float kingValue = 1000;

        float whiteEval = 0, blackEval = 0;

        foreach (Piece piece in board.GetPieceList(PieceType.Pawn, true))
        {
            whiteEval += pawnValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Knight, true))
        {
            whiteEval += knightValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Bishop, true))
        {
            whiteEval += bishopValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Rook, true))
        {
            whiteEval += rookValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Queen, true))
        {
            whiteEval += queenValue;
        }

        foreach (Piece piece in board.GetPieceList(PieceType.Pawn, false))
        {
            blackEval += pawnValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Knight, false))
        {
            blackEval += knightValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Bishop, false))
        {
            blackEval += bishopValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Rook, false))
        {
            blackEval += rookValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Queen, false))
        {
            blackEval += queenValue;
        }

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                whiteEval += kingValue;
            else
                blackEval += kingValue;
        }
        else if (board.IsInCheck())
        {
            if (board.IsWhiteToMove)
                whiteEval += kingValue / 2;
            else
                blackEval += kingValue / 2;
        }

        return (int)(whiteEval - blackEval);
    }
}