using System.Collections.Generic;
using UnityEngine;

public class RhythmGenerator {
    private List<NoteType> easyPool = new List<NoteType>() {
        NoteType.Quarter
    };

    private List<NoteType> normalPool = new List<NoteType>() {
        NoteType.Quarter, NoteType.Eighth
    };

    private List<NoteType> hardPool = new List<NoteType>() {
        NoteType.Quarter, NoteType.Eighth, NoteType.Sixteenth,
        NoteType.RestQuarter, NoteType.RestEighth, NoteType.RestSixteenth
    };

    public List<NoteType> GeneratePattern(float totalBeats, string difficulty) {
        List<NoteType> pool = 
            (difficulty == "easy") ? easyPool :
            (difficulty == "normal") ? normalPool :
            hardPool;

        List<NoteType> pattern = new List<NoteType>();
        float sum = 0f;

        // Safety break to prevent infinite loops if something goes wrong
        int maxIterations = 100;
        int iterations = 0;

        while (sum < totalBeats && iterations < maxIterations) {
            iterations++;
            NoteType candidate = pool[Random.Range(0, pool.Count)];
            float beatValue = NoteValue.GetBeats(candidate);

            if (sum + beatValue <= totalBeats) {
                pattern.Add(candidate);
                sum += beatValue;
            }
        }

        return pattern;
    }
}
