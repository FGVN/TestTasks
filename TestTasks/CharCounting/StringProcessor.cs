using System.Collections.Generic;
using System.Linq;

namespace TestTasks.VowelCounting
{
    public class StringProcessor
    {
        public (char symbol, int count)[] GetCharCount(string veryLongString, char[] countedChars)
        {
            var letterCounts = new Dictionary<char, int>();

            foreach (char c in countedChars)
                letterCounts[c] = 0;

            foreach (char c in veryLongString)
            {
                if (letterCounts.ContainsKey(c))
                    letterCounts[c]++;
            }

            return letterCounts.Select(x => (x.Key, x.Value)).ToArray();
        }
    }
}
