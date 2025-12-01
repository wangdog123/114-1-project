using System.Collections.Generic;

public enum NoteType {
    Quarter,        // 1
    Eighth,         // 0.5
    Sixteenth,      // 0.25
    RestQuarter,
    RestEighth,
    RestSixteenth
}

public static class NoteValue {
    public static float GetBeats(NoteType note) {
        switch(note) {
            case NoteType.Quarter: return 1f;
            case NoteType.Eighth: return 0.5f;
            case NoteType.Sixteenth: return 0.25f;
            
            case NoteType.RestQuarter: return 1f;
            case NoteType.RestEighth: return 0.5f;
            case NoteType.RestSixteenth: return 0.25f;
        }
        return 1f;
    }
}
