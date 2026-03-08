using System;
using System.Collections.Generic;

namespace VirtuLingo.Backend
{
    /// <summary>
    /// Data models matching the Python backend schema
    /// </summary>
    
    [Serializable]
    public class GrammarCorrectionData
    {
        public bool mistake_found;
        public string category;
        public string original;
        public string correction;
        public string explanation;
        public int severity;
    }
    
    [Serializable]
    public class GrammarCorrectionEvent
    {
        public string type;
        public string player_id;
        public GrammarCorrectionData data;
        public string timestamp;
    }
    
    [Serializable]
    public class WebSocketMessage
    {
        public string type;
        public string text;
        public bool enabled;
    }
    
    [Serializable]
    public class GameEvent
    {
        public string player_id;
        public string event_type;
        public Dictionary<string, object> payload;
    }
    
    [Serializable]
    public class ReviewCheckRequest
    {
        public string player_id;
        public int active_minutes;
    }
    
    [Serializable]
    public class ReviewCheckResponse
    {
        public bool trigger_review;
        public ReviewSession session;
    }
    
    [Serializable]
    public class ReviewSession
    {
        public string player_id;
        public List<string> top_mistake_categories;
        public List<ExerciseData> exercises;
        public string generated_at;
    }
    
    [Serializable]
    public class ExerciseData
    {
        public string type;
        public string question;
        public string correct_answer;
        public List<string> options;
    }
    
    [Serializable]
    public class PlayerState
    {
        public string player_id;
        public string language;
        public string proficiency_level;
        public string scene_id;
        public Position position;
        public string object_in_hand;
        public List<string> nearby_npcs;
        public string active_quest;
        public string active_npc_id;
    }
    
    [Serializable]
    public class Position
    {
        public float x;
        public float y;
        public float z;
    }
}
