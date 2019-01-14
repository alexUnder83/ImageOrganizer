using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageOrganizer {
    class PathTree : IEnumerable<string> {
        class Node {
            readonly List<Node> children = new List<Node>();
            public Node(string value) {
                Value = value;
            }

            public List<Node> Children { get { return children; } }
            public string Value { get; set; }
            public bool IsLeaf { get { return Children.Count == 0; } }

        }

        readonly Node root = new Node(string.Empty);
        readonly ManualResetEvent renamingEvent = new ManualResetEvent(true);
        int count;

        public bool IsEmpty { get { return root.Children.Count == 0; } }
        public int Count { get { return count; } }

        public void Add(string path) {
            string[] parts = path.Split(Path.DirectorySeparatorChar);
            Node current = root;
            for (int i = 0; i < parts.Length; i++) {
                string value = parts[i];
                Node child = current.Children.Find(node => node.Value == value);
                if (child == null) {
                    child = new Node(value);
                    current.Children.Add(child);
                }
                current = child;
            }
            this.count++;
        }
        public bool Contains(string path) {
            foreach (string value in Enumerate()) {
                if (value == path)
                    return true;
            }
            return false;
        }
        public void Clear() {
            this.root.Children.Clear();
            this.count = 0;
        }
        IEnumerable<string> ProcessChildren(Node node, string rootPath) {
            foreach (Node child in node.Children) {
                renamingEvent.WaitOne();
                string value;
                if (string.IsNullOrEmpty(rootPath))
                    value = child.Value;
                else
                    value = string.Join(Path.DirectorySeparatorChar.ToString(), rootPath, child.Value);

                if (child.IsLeaf)
                    yield return value;
                else {
                    foreach (string val in ProcessChildren(child, value))
                        yield return val;
                }
            }
        }
        IEnumerable<string> Enumerate() {
            return ProcessChildren(this.root, string.Empty);
        }
        public IEnumerator<string> GetEnumerator() {
            return Enumerate().GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        public void Rename(string from, string to) {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return;

            string[] fromParts = from.Split(Path.DirectorySeparatorChar);
            string[] toParts = to.Split(Path.DirectorySeparatorChar);
            if (fromParts.Length != toParts.Length)
                return;

            renamingEvent.Reset();
            Node current = root;
            for (int i = 0; i < fromParts.Length; i++) {
                string value = fromParts[i];
                Node child = current.Children.Find(node => node.Value == value);
                if (child == null)
                    break;

                child.Value = toParts[i];
                current = child;
            }
            renamingEvent.Set();
        }
    }
}
