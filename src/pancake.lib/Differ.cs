using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    //based on code from https://github.com/mmanela/diffplex
    public class Differ<T>
    {
        private static class Log
        {
            public static void WriteLine(string s, params object[] p)
            {
                //Debug.Write(String.Format(s, p));
            }

            public static void Write(string s, params object[] p)
            {
                //Debug.Write(String.Format(s, p));
            }
        }

        private static readonly string[] emptyStringArray = new string[0];

        /// <summary>
        /// Gets the default singleton instance of differ instance.
        /// </summary>
        public static Differ<T> Instance { get; } = new Differ<T>();

        public void ApplyDiffsToOld(IList<T> oldList, IList<T> newList, Func<T, string> getId)
        {
            var changes = Differ<T>.Instance.CreateDiffs(oldList, newList, getId);

            foreach (var c in changes.DiffBlocks.Reverse())
            {
                for (int i = 0; i < c.DeleteCountA; i++)
                    oldList.RemoveAt(c.DeleteStartA);

                for (int i = 0; i < c.InsertCountB; i++)
                    oldList.Insert(c.DeleteStartA + i, newList[c.InsertStartB + i]);
            }
        }


        public DiffResult CreateDiffs(IList<T> oldList, IList<T> newList, Func<T, string> getChunk)
        {
            if (oldList == null) throw new ArgumentNullException(nameof(oldList));
            if (newList == null) throw new ArgumentNullException(nameof(newList));
            if (getChunk == null) throw new ArgumentNullException(nameof(getChunk));

            var pieceHash = new Dictionary<string, int>(StringComparer.Ordinal);
            var lineDiffs = new List<DiffBlock>();

            var modOld = new ModificationData<T>(oldList);
            var modNew = new ModificationData<T>(newList);

            BuildPieceHashes(pieceHash, modOld, getChunk);
            BuildPieceHashes(pieceHash, modNew, getChunk);

            BuildModificationData(modOld, modNew);

            int piecesALength = modOld.HashedPieces!.Length;
            int piecesBLength = modNew.HashedPieces!.Length;
            int posA = 0;
            int posB = 0;

            do
            {
                while (posA < piecesALength
                       && posB < piecesBLength
                       && !modOld.Modifications![posA]
                       && !modNew.Modifications![posB])
                {
                    posA++;
                    posB++;
                }

                int beginA = posA;
                int beginB = posB;
                for (; posA < piecesALength && modOld.Modifications[posA]; posA++) ;

                for (; posB < piecesBLength && modNew.Modifications[posB]; posB++) ;

                int deleteCount = posA - beginA;
                int insertCount = posB - beginB;
                if (deleteCount > 0 || insertCount > 0)
                {
                    lineDiffs.Add(new DiffBlock(beginA, deleteCount, beginB, insertCount));
                }
            } while (posA < piecesALength && posB < piecesBLength);

            return new DiffResult(modOld.Pieces, modNew.Pieces, lineDiffs);
        }

        /// <summary>
        /// Finds the middle snake and the minimum length of the edit script comparing string A and B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="startA">Lower bound inclusive</param>
        /// <param name="endA">Upper bound exclusive</param>
        /// <param name="B"></param>
        /// <param name="startB">lower bound inclusive</param>
        /// <param name="endB">upper bound exclusive</param>
        /// <returns></returns>
        protected static EditLengthResult CalculateEditLength(int[] A, int startA, int endA, int[] B, int startB, int endB)
        {
            int N = endA - startA;
            int M = endB - startB;
            int MAX = M + N + 1;

            var forwardDiagonal = new int[MAX + 1];
            var reverseDiagonal = new int[MAX + 1];
            return CalculateEditLength(A, startA, endA, B, startB, endB, forwardDiagonal, reverseDiagonal);
        }

        private static EditLengthResult CalculateEditLength(int[] A, int startA, int endA, int[] B, int startB, int endB, int[] forwardDiagonal, int[] reverseDiagonal)
        {
            if (null == A) throw new ArgumentNullException(nameof(A));
            if (null == B) throw new ArgumentNullException(nameof(B));

            if (A.Length == 0 && B.Length == 0)
            {
                return new EditLengthResult();
            }

            int N = endA - startA;
            int M = endB - startB;
            int MAX = M + N + 1;
            int HALF = MAX / 2;
            int delta = N - M;
            bool deltaEven = delta % 2 == 0;
            forwardDiagonal[1 + HALF] = 0;
            reverseDiagonal[1 + HALF] = N + 1;

            Log.WriteLine("Comparing strings");
            Log.WriteLine("\t{0} of length {1}", A, A.Length);
            Log.WriteLine("\t{0} of length {1}", B, B.Length);

            for (int D = 0; D <= HALF; D++)
            {
                Log.WriteLine("\nSearching for a {0}-Path", D);
                // forward D-path
                Log.WriteLine("\tSearching for forward path");
                Edit lastEdit;
                for (int k = -D; k <= D; k += 2)
                {
                    Log.WriteLine("\n\t\tSearching diagonal {0}", k);
                    int kIndex = k + HALF;
                    int x, y;
                    if (k == -D || (k != D && forwardDiagonal[kIndex - 1] < forwardDiagonal[kIndex + 1]))
                    {
                        x = forwardDiagonal[kIndex + 1]; // y up    move down from previous diagonal
                        lastEdit = Edit.InsertDown;
                        Log.Write("\t\tMoved down from diagonal {0} at ({1},{2}) to ", k + 1, x, (x - (k + 1)));
                    }
                    else
                    {
                        x = forwardDiagonal[kIndex - 1] + 1; // x up     move right from previous diagonal
                        lastEdit = Edit.DeleteRight;
                        Log.Write("\t\tMoved right from diagonal {0} at ({1},{2}) to ", k - 1, x - 1, (x - 1 - (k - 1)));
                    }
                    y = x - k;
                    int startX = x;
                    int startY = y;
                    Log.WriteLine("({0},{1})", x, y);
                    while (x < N && y < M && A[x + startA] == B[y + startB])
                    {
                        x += 1;
                        y += 1;
                    }
                    Log.WriteLine("\t\tFollowed snake to ({0},{1})", x, y);

                    forwardDiagonal[kIndex] = x;

                    if (!deltaEven && k - delta >= -D + 1 && k - delta <= D - 1)
                    {
                        int revKIndex = (k - delta) + HALF;
                        int revX = reverseDiagonal[revKIndex];
                        int revY = revX - k;
                        if (revX <= x && revY <= y)
                        {
                            return new EditLengthResult
                            {
                                EditLength = 2 * D - 1,
                                StartX = startX + startA,
                                StartY = startY + startB,
                                EndX = x + startA,
                                EndY = y + startB,
                                LastEdit = lastEdit
                            };
                        }
                    }
                }

                // reverse D-path
                Log.WriteLine("\n\tSearching for a reverse path");
                for (int k = -D; k <= D; k += 2)
                {
                    Log.WriteLine("\n\t\tSearching diagonal {0} ({1})", k, k + delta);
                    int kIndex = k + HALF;
                    int x, y;
                    if (k == -D || (k != D && reverseDiagonal[kIndex + 1] <= reverseDiagonal[kIndex - 1]))
                    {
                        x = reverseDiagonal[kIndex + 1] - 1; // move left from k+1 diagonal
                        lastEdit = Edit.DeleteLeft;
                        Log.Write("\t\tMoved left from diagonal {0} at ({1},{2}) to ", k + 1, x + 1, ((x + 1) - (k + 1 + delta)));
                    }
                    else
                    {
                        x = reverseDiagonal[kIndex - 1]; //move up from k-1 diagonal
                        lastEdit = Edit.InsertUp;
                        Log.Write("\t\tMoved up from diagonal {0} at ({1},{2}) to ", k - 1, x, (x - (k - 1 + delta)));
                    }
                    y = x - (k + delta);

                    int endX = x;
                    int endY = y;

                    Log.WriteLine("({0},{1})", x, y);
                    while (x > 0 && y > 0 && A[startA + x - 1] == B[startB + y - 1])
                    {
                        x -= 1;
                        y -= 1;
                    }

                    Log.WriteLine("\t\tFollowed snake to ({0},{1})", x, y);
                    reverseDiagonal[kIndex] = x;

                    if (deltaEven && k + delta >= -D && k + delta <= D)
                    {
                        int forKIndex = (k + delta) + HALF;
                        int forX = forwardDiagonal[forKIndex];
                        int forY = forX - (k + delta);
                        if (forX >= x && forY >= y)
                        {
                            return new EditLengthResult
                            {
                                EditLength = 2 * D,
                                StartX = x + startA,
                                StartY = y + startB,
                                EndX = endX + startA,
                                EndY = endY + startB,
                                LastEdit = lastEdit
                            };
                        }
                    }
                }
            }

            throw new Exception("Should never get here");
        }

        protected static void BuildModificationData(ModificationData<T> A, ModificationData<T> B)
        {
            int N = A.HashedPieces!.Length;
            int M = B.HashedPieces!.Length;
            int MAX = M + N + 1;
            var forwardDiagonal = new int[MAX + 1];
            var reverseDiagonal = new int[MAX + 1];
            BuildModificationData(A, 0, N, B, 0, M, forwardDiagonal, reverseDiagonal);
        }

        private static void BuildModificationData
            (ModificationData<T> A,
             int startA,
             int endA,
             ModificationData<T> B,
             int startB,
             int endB,
             int[] forwardDiagonal,
             int[] reverseDiagonal)
        {
            while (startA < endA && startB < endB && A.HashedPieces[startA].Equals(B.HashedPieces[startB]))
            {
                startA++;
                startB++;
            }
            while (startA < endA && startB < endB && A.HashedPieces[endA - 1].Equals(B.HashedPieces[endB - 1]))
            {
                endA--;
                endB--;
            }

            int aLength = endA - startA;
            int bLength = endB - startB;
            if (aLength > 0 && bLength > 0)
            {
                EditLengthResult res = CalculateEditLength(A.HashedPieces, startA, endA, B.HashedPieces, startB, endB, forwardDiagonal, reverseDiagonal);
                if (res.EditLength <= 0) return;

                if (res.LastEdit == Edit.DeleteRight && res.StartX - 1 > startA)
                    A.Modifications[--res.StartX] = true;
                else if (res.LastEdit == Edit.InsertDown && res.StartY - 1 > startB)
                    B.Modifications[--res.StartY] = true;
                else if (res.LastEdit == Edit.DeleteLeft && res.EndX < endA)
                    A.Modifications[res.EndX++] = true;
                else if (res.LastEdit == Edit.InsertUp && res.EndY < endB)
                    B.Modifications[res.EndY++] = true;

                BuildModificationData(A, startA, res.StartX, B, startB, res.StartY, forwardDiagonal, reverseDiagonal);

                BuildModificationData(A, res.EndX, endA, B, res.EndY, endB, forwardDiagonal, reverseDiagonal);
            }
            else if (aLength > 0)
            {
                for (int i = startA; i < endA; i++)
                    A.Modifications[i] = true;
            }
            else if (bLength > 0)
            {
                for (int i = startB; i < endB; i++)
                    B.Modifications[i] = true;
            }
        }

        private static void BuildPieceHashes(IDictionary<string, int> pieceHash, ModificationData<T> data, Func<T, string> getChunk)
        {
            var pieces = data.RawData?.Select(r => getChunk(r)).ToArray() ?? emptyStringArray;

            data.Pieces = pieces;
            data.HashedPieces = new int[pieces.Length];
            data.Modifications = new bool[pieces.Length];

            for (int i = 0; i < pieces.Length; i++)
            {
                string piece = pieces[i];

                if (pieceHash.ContainsKey(piece))
                {
                    data.HashedPieces[i] = pieceHash[piece];
                }
                else
                {
                    data.HashedPieces[i] = pieceHash.Count;
                    pieceHash[piece] = pieceHash.Count;
                }
            }
        }
    }

    public class DiffResult
    {
        /// <summary>
        /// The chunked pieces of the old text
        /// </summary>
        public string[] PiecesOld { get; }

        /// <summary>
        /// The chunked pieces of the new text
        /// </summary>
        public string[] PiecesNew { get; }


        /// <summary>
        /// A collection of DiffBlocks which details deletions and insertions
        /// </summary>
        public IList<DiffBlock> DiffBlocks { get; }

        public DiffResult(string[] peicesOld, string[] piecesNew, IList<DiffBlock> blocks)
        {
            PiecesOld = peicesOld;
            PiecesNew = piecesNew;
            DiffBlocks = blocks;
        }
    }

    public class ModificationData<T>
    {
        public int[] HashedPieces { get; set; } = new int[0];

        public IList<T>? RawData { get; }

        public bool[] Modifications { get; set; } = new bool[0];

        public string[] Pieces { get; set; } = new string[0];

        public ModificationData(IList<T> list)
        {
            RawData = list;
        }
    }

    /// <summary>
    /// A block of consecutive edits from A and/or B
    /// </summary>
    public class DiffBlock
    {
        /// <summary>
        /// Position where deletions in A begin
        /// </summary>
        public int DeleteStartA { get; }

        /// <summary>
        /// The number of deletions in A
        /// </summary>
        public int DeleteCountA { get; }

        /// <summary>
        /// Position where insertion in B begin
        /// </summary>
        public int InsertStartB { get; }

        /// <summary>
        /// The number of insertions in B
        /// </summary>
        public int InsertCountB { get; }


        public DiffBlock(int deleteStartA, int deleteCountA, int insertStartB, int insertCountB)
        {
            DeleteStartA = deleteStartA;
            DeleteCountA = deleteCountA;
            InsertStartB = insertStartB;
            InsertCountB = insertCountB;
        }
    }

    public interface IChunker<T>
    {
        /// <summary>
        /// Divide text into sub-parts
        /// </summary>
        string[] Chunk(IList<T> list);
    }

    public class EditLengthResult
    {
        public int EditLength { get; set; }

        public int StartX { get; set; }
        public int EndX { get; set; }
        public int StartY { get; set; }
        public int EndY { get; set; }

        public Edit LastEdit { get; set; }
    }

    public enum Edit
    {
        None,
        DeleteRight,
        DeleteLeft,
        InsertDown,
        InsertUp
    }
}