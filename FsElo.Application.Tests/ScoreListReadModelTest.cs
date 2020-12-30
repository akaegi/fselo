using System;
using System.Linq;
using FsElo.Domain.Scoreboard.Events;
using FsElo.WebApp.Application;
using Xunit;

namespace FsElo.Application.Tests
{
    public class ScoreListReadModelTest
    {
        private readonly Guid _score1Id;
        private readonly Guid _score2Id;
        private readonly Guid _score3Id;
        private readonly Guid _score4Id;
        private readonly Tuple<Guid, Guid> _players12;
        private readonly Tuple<Guid, Guid> _players13;
        private readonly DateTimeOffset _dt1;
        private readonly DateTimeOffset _dt2;

        public ScoreListReadModelTest()
        {
            _score1Id = Guid.NewGuid();
            _score2Id = Guid.NewGuid();
            _score3Id = Guid.NewGuid();
            _score4Id = Guid.NewGuid();
            
            _players12 = (Guid.NewGuid(), Guid.NewGuid()).ToTuple();
            _players13 = (_players12.Item1, Guid.NewGuid()).ToTuple();

            _dt1 = DateTimeOffset.Now;
            _dt2 = _dt1.AddHours(1);
        }
        
        [Fact]
        public void TestScoreEntered()
        {
            var uut = new ScoreListReadModel();

            uut.Apply(Event.NewPlayerRegistered(new PlayerRegistered(_players12.Item1, PlayerName.NewPlayerName("dr"), _dt1)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score2Id, _players12, Score("2:2"), _dt2)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score1Id, _players12, Score("1:1"), _dt1)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score3Id, _players12.Swap(), Score("3:3"), _dt2)));

            var scoreList = uut.ScoreList("dr").ToList();
            
            Assert.Equal(3, scoreList.Count);
            
            Assert.Equal("3:3", scoreList[0].Score); // newest, added last
            Assert.Equal("2:2", scoreList[1].Score);
            Assert.Equal("1:1", scoreList[2].Score); // oldest
        }
        
        [Fact]
        public void TestScoreEnteredAgainstAdversary()
        {
            var uut = new ScoreListReadModel();

            uut.Apply(Event.NewPlayerRegistered(new PlayerRegistered(_players12.Item1, PlayerName.NewPlayerName("dr"), _dt1)));
            uut.Apply(Event.NewPlayerRegistered(new PlayerRegistered(_players13.Item2, PlayerName.NewPlayerName("lr"), _dt1)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score2Id, _players12, Score("2:2"), _dt2)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score1Id, _players12, Score("1:1"), _dt1)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score3Id, _players12.Swap(), Score("3:3"), _dt2)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score3Id, _players13, Score("4:4"), _dt1)));

            var scoreList = uut.ScoreList("dr", "lr").ToList();
            
            Assert.Single(scoreList);
            Assert.Equal("4:4", scoreList[0].Score); 
        }
        
        [Fact]
        public void TestScoreFixed()
        {
            var uut = new ScoreListReadModel();

            uut.Apply(Event.NewPlayerRegistered(new PlayerRegistered(_players12.Item1, PlayerName.NewPlayerName("dr"), _dt1)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score2Id, _players12, Score("2:2"), _dt2)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score1Id, _players12, Score("1:1"), _dt1)));
            uut.Apply(Event.NewScoreFixed(new ScoreEntered(_score2Id, _players12, Score("1:2"), _dt2)));

            var scoreList = uut.ScoreList("dr", "lr").ToList();
            
            Assert.Equal(2, scoreList.Count);
            Assert.Equal("1:2", scoreList[0].Score); // newest
        }

        [Fact]
        public void TestScoreRemoved()
        {
            var uut = new ScoreListReadModel();

            uut.Apply(Event.NewPlayerRegistered(new PlayerRegistered(_players12.Item1, PlayerName.NewPlayerName("dr"), _dt1)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score1Id, _players12, Score("1:1"), _dt1)));
            uut.Apply(Event.NewScoreEntered(new ScoreEntered(_score2Id, _players12, Score("2:2"), _dt2)));
            uut.Apply(Event.NewScoreWithdrawn(new ScoreWithdrawn(_score2Id, _dt2)));

            var scoreList = uut.ScoreList("dr", "lr").ToList();
            
            Assert.Single(scoreList);
            Assert.Equal("1:1", scoreList[0].Score);
        }
        
        private Tuple<int, int> Score(string inp)
        {
            string[] parts = inp.Split(':');
            return (int.Parse(parts[0]), int.Parse(parts[1])).ToTuple();
        }
    }

    static class TupleExtensions
    {
        public static Tuple<T2, T1> Swap<T1, T2>(this Tuple<T1, T2> me)
        {
            return (me.Item2, me.Item1).ToTuple();
        }
    }
}