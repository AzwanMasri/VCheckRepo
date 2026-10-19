//using HL7Listener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheckListenerWorker_Others.Lib.Object
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

        public String MessageControlId()
        {
            Segment msh = Header();
            if (msh == null) return String.Empty;
            return msh.Field(MSH_MSG_CONTROL_ID);
        }

        public void Add(Segment segment)
        {
            if (!String.IsNullOrEmpty(segment.Name) && segment.Name.Length == 3)
            {
                segments.Add(segment);
            }
        }

        public void DeSerializeMessage(String msg)
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

        public String SerializeMessage()
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
