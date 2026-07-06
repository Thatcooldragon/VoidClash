using UnityEngine;

namespace VoidClash
{
    /// <summary>Lightweight mission flavor beats. Keeps story text out of combat systems.</summary>
    public class StoryDirector : MonoBehaviour
    {
        MissionDef _mission;
        float _time;
        bool _sentBeat;

        public void Init(MissionDef mission)
        {
            _mission = mission;
            _time = 0f;
            _sentBeat = false;
        }

        void Update()
        {
            if (_mission == null || _sentBeat || G.Game == null || G.Game.IsPaused || G.Game.IsOver) return;
            _time += Time.deltaTime;
            if (_time < _mission.storyBeatTime || string.IsNullOrEmpty(_mission.storyBeatText)) return;
            _sentBeat = true;
            if (G.Hud != null) G.Hud.Notify(_mission.storyBeatText);
            if (G.Audio != null) G.Audio.PlayVoice("voice_warning");
        }
    }
}
