using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Stats
    {
        public int Time;
        public int Score;
        public int ShotsFired;
        public int ShotsSuccessful;
        public int MonstersDead;
        public int HitsTaken;
        public int LivesLost;

        public void Reset()
        {
            Time = Score = ShotsSuccessful = ShotsFired
                = MonstersDead = HitsTaken = LivesLost = default;
        }

        public void AddTo(Stats other)
        {
            other.MonstersDead += MonstersDead;
            other.Score += Score;
            other.ShotsFired += ShotsFired;
            other.ShotsSuccessful += ShotsSuccessful;
            other.Time += Time;
            other.HitsTaken += HitsTaken;
            other.LivesLost += LivesLost;
        }
    }
}
