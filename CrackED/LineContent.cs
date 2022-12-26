using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrackED
{
    public class LineContent
    {
        private readonly List<string> Chars = new List<string>();

        internal event EventHandler? OnContentChanged;

        public int TextLenght
        {
            get
            {
                return Chars.Count;
            }
        }

        public LineContent(List<string> content = null)
        {
            if(content != null)
            {
                Chars = content;
            }        
        }

        public void Apend(LineContent line)
        {
            Chars.AddRange(line.Chars);
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddRange(IEnumerable<string> range)
        {
            Chars.AddRange(range);
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void InsertRange(int index, IEnumerable<string> range)
        {
            Chars.InsertRange(index, range);
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveRange(int start, int count)
        {
            Chars.RemoveRange(start, count);
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public List<string> GetRange(int start, int lenght)
        {
            return Chars.GetRange(start, lenght);
        }

        public void Add(string item)
        {
            Chars.Add(item);
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Insert(int index, string item)
        {
            Chars.Insert(index, item);
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveAt(int start)
        {
            Chars.RemoveAt(start);
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            Chars.Clear();
            OnContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public dynamic CountDistinct()
        {
            return Chars
            .GroupBy(s => s)
            .Select(g => new { Text = g.Key, Count = g.Count() });
        }

        public string ToText()
        {
            return string.Join("", Chars);
        }
    }
}
