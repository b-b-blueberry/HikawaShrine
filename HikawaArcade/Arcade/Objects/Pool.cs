using System;

namespace HikawaArcade.Arcade.Objects
{
    public class Pool<T> where T : class, new()
    {
        private class Node
        {
            public int Index;
            public T Value;
            public Node Next = null;


            public Node()
            {
                Value = new T();
            }

            public Node(int index)
                : this()
            {
                Index = index;
            }

            public Node(T value)
            {
                Value = value;
            }
        }

        public enum Strategy
        {
            Pass,
            Replace,
            Throw
        }

        public readonly int Capacity;
        private readonly bool[] InUse = null;
        private readonly Node[] Array = null;
        private Node FirstAvailable = null;


        public Pool(int capacity)
        {
            Capacity = capacity;
            Array = new Node[capacity];
            InUse = new bool[capacity];
            FirstAvailable = Array[0] = new Node(index: 0);
            for (int i = 1; i < capacity; ++i)
            {
                Array[i] = new Node(index: i);
                Array[i - 1].Next = Array[i];
            }
        }

        public bool IsFull => FirstAvailable == null;

        private void Use(Node node)
        {
            InUse[node.Index] = true;
            FirstAvailable = node.Next;
        }

        private void UseFirst(T value)
        {
            Node node = FirstAvailable;
            node.Value = value;
            Use(node: node);
        }

        private void Free(Node node)
        {
            InUse[node.Index] = false;
            node.Next = FirstAvailable;
            FirstAvailable = node;
        }

        public T Get(Strategy strategy = Strategy.Pass)
        {
            if (!IsFull || strategy == Strategy.Replace)
            {
                Node node = FirstAvailable;
                UseFirst(value: node.Value);
                return node.Value;
            }
            else if (strategy == Strategy.Throw)
            {
                throw new Exception($"Failed to get value from {this} {typeof(T)}");
            }
            // Strategy.Pass
            return null;
        }

        public T Add(T value, Strategy strategy = Strategy.Pass)
        {
            if (!IsFull || strategy == Strategy.Replace)
            {
                UseFirst(value: value);
            }
            else if (strategy == Strategy.Throw)
            {
                throw new Exception($"Failed to add value {value} to {this} {typeof(T)}");
            }
            else // Strategy.Pass
            {
                return null;
            }
            return value;
        }

        public void ForEach(Action<T> action)
        {
            for (int i = 0; i < Capacity; ++i)
            {
                if (InUse[i])
                {
                    action.Invoke(Array[i].Value);
                }
            }
        }

        public void ForEach(Func<T, bool> action)
        {
            for (int i = 0; i < Capacity; ++i)
            {
                if (InUse[i] && action.Invoke(Array[i].Value))
                {
                    Free(node: Array[i]);
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < Capacity; ++i)
            {
                InUse[i] = false;
            }
            FirstAvailable = Array[0];
        }
    }
}
