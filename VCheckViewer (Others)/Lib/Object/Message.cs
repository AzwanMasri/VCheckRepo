//using HL7Listener;
using System.Text;

namespace VCheckViewerAPI.Lib.Object
{
    public class Message
    {
        private const string MSH = "MSH";
        private const int MSH_MSG_CONTROL_ID = 10;

        private List<Segment> segments;

        public Message()
        {
            Initialize();
        }

        public void Initialize()
        {
            segments = new List<Segment>();
        }

        protected Segment Header()
        {
            if (segments.Count == 0 || segments[0].Name != MSH)
            {
                return null;
            }
            return segments[0];
        }

        public string MessageControlId()
        {
            Segment msh = Header();
            if (msh == null) return string.Empty;
            return msh.Field(MSH_MSG_CONTROL_ID);
        }

        public void Add(Segment segment)
        {
            if (!string.IsNullOrEmpty(segment.Name) && segment.Name.Length == 3)
            {
                segments.Add(segment);
            }
        }

        public void DeSerializeMessage(string msg)
        {
            Initialize();

            char[] separator = { '\r' };
            var tokens = msg.Split(separator, StringSplitOptions.None);

            foreach (var item in tokens)
            {
                var segment = new Segment();
                segment.DeSerializedSegment(item.Trim('\n'));
                Add(segment);
            }
        }

        public string SerializeMessage()
        {
            var builder = new StringBuilder();
            char[] separators = { '\r', '\n' };

            foreach (var segment in segments)
            {
                builder.Append(segment.SerializeSegment());
                builder.Append("\r\n");
            }
            return builder.ToString().TrimEnd(separators);
        }
    }
}
