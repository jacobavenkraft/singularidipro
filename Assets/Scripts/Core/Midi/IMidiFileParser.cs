using System.Collections.Generic;

namespace Singularidi.Midi
{
    public interface IMidiFileParser
    {
        (List<NoteEvent> Notes, double TotalDurationSeconds) Parse(string path);
    }
}
