using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheckListenerWorker_Others.Lib.Object
{
    public class Segment
    {
        private Dictionary<int, String> fields;

        public Segment()
        {
            fields = new Dictionary<int, string>(100);
        }

        public Segment(String name)
        {
            fields = new Dictionary<int, string>(100);
            fields.Add(0, name);
        }

        public String Name
        {
            get
            {
                if (!fields.ContainsKey(0))
                {
                    return String.Empty;
                }

                return fields[0];
            }
        }

        public String Field(int key)
        {
            if (Name == "MSH" && key == 1) return "|";

            if (!fields.ContainsKey(key))
            {
                return String.Empty;
            }

            return fields[key];
        }

        public void Field(int key, String value)
        {
            if (Name == "MSH" && key == 1) return;

            if (!String.IsNullOrEmpty(value))
            {
                if (fields.ContainsKey(key))
                {
                    fields.Remove(key);
                }

                fields.Add(key, value);
            }
        }

        public void DeSerializedSegment(string segment)
        {
            int count = 0;
            char[] separators = { '|' };

            string temp = segment.Trim('|');
            string[] fields = temp.Split(separators, StringSplitOptions.None);

            foreach (var field in fields)
            {
                Field(count, field);
                if (field == "MSH")
                {
                    ++count;
                }
                ++count;
            }
        }

        public String SerializeSegment()
        {
            int max = 0;
            foreach (var field in fields)
            {
                if (max < field.Key)
                {
                    max = field.Key;
                }
            }

            StringBuilder temp = new StringBuilder();

            for (int index = 0; index <= max; index++)
            {
                if (fields.ContainsKey(index))
                {
                    temp.Append(fields[index]);

                    if (index == 0 && Name == "MSH")
                    {
                        ++index;
                    }
                }
                if (index != max) temp.Append("|");
            }

            return temp.ToString();
        }
    }
}
