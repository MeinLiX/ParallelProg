using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Parallel_BFS_dotNET
{
    class NodeTree
    {
        public string Name { get; }
        public List<NodeTree> Children { get; }
        public bool Visited { get; private set; }

        public NodeTree(string name)
        {
            Name = name;
            Children = new();
        }


        public NodeTree AddChildren(NodeTree node)
        {
            if (node != null)
                Children.Add(node);
            return this;
        }

        public IEnumerable<NodeTree> NextChildren()
        {
            foreach (var item in Children)
                yield return item;
        }

        //MinChildrens - this is the minimum number of children in Vertex
        public IEnumerable<NodeTree> DeepNextChildren(int MinChildrens = 2)
        {
            foreach (var item in Children)
                if (item.Children.Count >= MinChildrens)
                    foreach (var deepitem in item.DeepNextChildren())
                        yield return deepitem;
                else yield return item;
        }

        public NodeTree GenerateTree(int CountOfVertex)
        {
            int i = 1;
            if (Children.Count != 0)
                throw new Exception("There should be no children in the main node!");
            AddChildren(new($"{++i}"));
            AddChildren(new($"{++i}"));

            while (i < CountOfVertex)
            {
                var currentNodes = DeepNextChildren();
                List<NodeTree> Children = new();
                foreach (var item in currentNodes)
                    Children.Add(item);

                foreach (var item in Children)
                {
                    item.AddChildren(new($"{++i}"));
                    item.AddChildren(new($"{++i}"));
                }
            }
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"{this.Name} \t ({Visited}) --> ");
            foreach (var node in NextChildren())
                sb.Append($"{node.Name}  \t");
            return sb.ToString();
        }

        public string ToString(bool details)
        {
            if (details)
            {
                StringBuilder sb = new();
                sb.Append($"{ToString()} \n");
                foreach (var node in NextChildren())
                    if (node.Children.Count > 0)
                        sb.Append(node.ToString(true));
                return sb.ToString();
            }
            else return ToString();
        }

        public void VisiteDeepP(bool flag = true)
        {
            Visited = flag;
            Parallel.ForEach(NextChildren(),
               (node) =>
               {
                   if (node.Children.Count > 0)
                       node.VisiteDeep(flag);
                   else
                       node.Visited = flag;
               });
        }

        public void VisiteDeep(bool flag = true)
        {
            Visited = flag;
            foreach (var node in NextChildren())
            {
                if (node.Children.Count > 0)
                    node.VisiteDeep(flag);
                else
                    node.Visited = flag;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            NodeTree graph = new("main");
            Stopwatch watchG = new();
            watchG.Start();
            graph.GenerateTree(10000000);
            watchG.Stop();

            Stopwatch watchP = new();
            watchP.Start();
            graph.VisiteDeepP(true);
            watchP.Stop();

            Console.WriteLine($"Final. Time Generation(ms): {watchG.Elapsed.TotalMilliseconds} | Time Parallel(ms): {watchP.Elapsed.TotalMilliseconds}.");
        }
    }
}
