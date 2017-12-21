using System.Collections.Generic;
using System.Text;

public class UnionFind<T>
{

    public class SetElement
    {
        public SetElement parent;
        public readonly T value;
        public int size;

        public SetElement(T value)
        {
            this.value = value;
            parent = this;
            size = 1;
        }

        override public string ToString()
        {
            return string.Format("{0}, size:{1}", value, size);
        }
    }

    Dictionary<T, SetElement> dict;

    public UnionFind()
    {
        dict = new Dictionary<T, SetElement>();
    }

    public SetElement MakeSet(T value)
    {
        SetElement element = new SetElement(value);
        dict[value] = element;
        return element;
    }

    public SetElement Find(T value)
    {
        SetElement element = dict[value];
        SetElement root = element;
        while (root.parent != root)
        {
            root = root.parent;
        }
        SetElement z = element;
        while (z.parent != z)
        {
            SetElement temp = z;
            z = z.parent;
            temp.parent = root;
        }
        return root;
    }

    public SetElement Union(SetElement root1, SetElement root2)
    {
        if (root2.size > root1.size)
        {
            root2.size += root1.size;
            root1.parent = root2;
            return root2;
        }
        else
        {
            root1.size += root2.size;
            root2.parent = root1;
            return root1;
        }
    }

    public SetElement UnionValues(T v1, T v2)
    {
        return Union(Find(v1),Find(v2));
    }

    override public string ToString()
    {
        StringBuilder sb = new StringBuilder();
        HashSet<T> already_printed = new HashSet<T>();
        foreach (T v in dict.Keys)
        {
            SetElement r = Find(v);
            if (!already_printed.Contains(r.value))
            {
                sb.Append(r.value.ToString() + " : " + r.size + "\n");
                already_printed.Add(r.value);
            }
        }
        return sb.ToString();
    }
}
