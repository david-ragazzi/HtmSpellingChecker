using System;
using System.Collections.Generic;
using SpellingChecker.Common;

namespace Evaluation
{
    class Global
    {
        public enum SpellingMethod
        {
            Levenshtein,
            NGram,
            Htm
        }

        public static SpellingChecker.Levenshtein.Engine LevenshteinSP = new SpellingChecker.Levenshtein.Engine();
        public static SpellingChecker.NGram.Engine NGramSP = new SpellingChecker.NGram.Engine();
        public static SpellingChecker.HTM.Engine HtmSP = new SpellingChecker.HTM.Engine();
    }
}
