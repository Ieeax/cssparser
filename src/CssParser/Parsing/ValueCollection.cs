using System;
using System.Collections;
using System.Collections.Generic;

namespace Leeax.Parsing.CSS
{
    public class ValueCollection : IEnumerable<IValue>
    {
        private readonly List<IValue> _values;

        public ValueCollection()
        {
            _values = new List<IValue>();
        }

        public IValue this[int index]
        {
            get
            {
                if (index < 0 || _values.Count <= index)
                {
                    throw new IndexOutOfRangeException("Index was out of range. Must be non-negative and less than the size of the collection.");
                }

                return _values[index];
            }
        }

        public void AddValue(IValue componentValue)
        {
            _values.Add(componentValue);
        }

        public void RemoveValue(int index)
        {
            if (index < 0 || _values.Count <= index)
            {
                throw new IndexOutOfRangeException("Index was out of range. Must be non-negative and less than the size of the collection.");
            }

            _values.RemoveAt(index);
        }

        public override string ToString()
        {
            return string.Concat(_values);
        }

        public string ToString(bool trimWhitesapce)
        {
            return trimWhitesapce
                ? ToString().Trim(' ')
                : ToString();
        }

        public IEnumerator<IValue> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _values.Count;
    }
}