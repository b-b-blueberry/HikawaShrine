using System.Collections.Generic;

namespace Hikawa.Volleyball
{
    public struct VolleyballRules(string ballType, IEnumerable<Character> players, int scoreGoal, bool isDoubles)
    {
        public string BallType = ballType;
        public IEnumerable<Character> Players = players;
        public int ScoreGoal = scoreGoal;
        public bool IsDoubles = isDoubles;
    }
}
