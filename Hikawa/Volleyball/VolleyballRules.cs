using System.Collections.Generic;

namespace Hikawa.Volleyball
{
    public struct VolleyballRules(IEnumerable<Character> players, int scoreGoal, bool isDoubles)
    {
        public IEnumerable<Character> Players = players;
        public int ScoreGoal = scoreGoal;
        public bool IsDoubles = isDoubles;
    }
}
