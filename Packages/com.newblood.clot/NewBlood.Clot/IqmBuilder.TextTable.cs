using System.Text;
using System.Collections.Generic;

namespace NewBlood.Clot
{
    public partial class IqmBuilder
    {
		class TextTable
        {
            readonly List<byte> table;
            readonly Dictionary<string, int> indices;

            public TextTable()
            {
                table   = new List<byte>();
                indices = new Dictionary<string, int>();
                Reset();
            }

            public int GetOrAddIndex(string text)
            {
                // Remap null to the empty string for convenience purposes
                text = text ?? string.Empty;

                if (indices.TryGetValue(text, out int index))
                    return index;

                var bytes = Encoding.UTF8.GetBytes(text);
                index     = table.Count;

                indices.Add(text, index);
                table.AddRange(bytes);
                table.Add(0);

                return index;
            }

            public void Reset()
            {
                table.Clear();
                indices.Clear();

                // Ensure empty string is first
                GetOrAddIndex(string.Empty);
            }

            public byte[] ToArray()
            {
                return table.ToArray();
            }
        }
    }
}
